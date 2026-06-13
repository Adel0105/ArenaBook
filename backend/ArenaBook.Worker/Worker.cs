using System.Text;
using System.Text.Json;
using ArenaBook.Application.Abstractions.Messaging;
using ArenaBook.Application.Options;
using ArenaBook.Domain.Entities;
using ArenaBook.Domain.Messaging;
using ArenaBook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ArenaBook.Worker;

public sealed class Worker : BackgroundService
{
    private const string QueueName = "arena-book.events";

    private readonly ILogger<Worker> _logger;
    private readonly RabbitMqOptions _options;
    private readonly IDbContextFactory<ArenaBookDbContext> _dbFactory;
    private readonly IEmailSender _emailSender;
    private int _connectAttempt;

    public Worker(
        ILogger<Worker> logger,
        IOptions<RabbitMqOptions> options,
        IDbContextFactory<ArenaBookDbContext> dbFactory,
        IEmailSender emailSender)
    {
        _logger = logger;
        _options = options.Value;
        _dbFactory = dbFactory;
        _emailSender = emailSender;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunConsumerAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _connectAttempt++;
                var delaySeconds = Math.Min(60, (int)Math.Pow(2, Math.Min(_connectAttempt, 6)));
                _logger.LogError(ex, "RabbitMQ consumer stopped unexpectedly; retrying in {DelaySeconds}s", delaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
            }
        }
    }

    private async Task RunConsumerAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = string.IsNullOrEmpty(_options.VirtualHost) ? "/" : _options.VirtualHost,
        };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var text = Encoding.UTF8.GetString(body);
                await HandleMessageAsync(text, stoppingToken);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
            }
            catch (Exception ex)
            {
                if (ea.Redelivered)
                {
                    _logger.LogError(ex, "Failed to process message after redelivery; discarding");
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, stoppingToken);
                }
                else
                {
                    _logger.LogWarning(ex, "Failed to process message; requeueing once");
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }
        };

        await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer, stoppingToken);

        _connectAttempt = 0;
        _logger.LogInformation("Worker subscribed to queue {Queue} on {Host}:{Port}", QueueName, _options.Host, _options.Port);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task HandleMessageAsync(string json, CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (!root.TryGetProperty("kind", out var kindEl))
            return;

        var kind = kindEl.GetString();
        if (string.Equals(kind, RabbitMessageKinds.UserNotification, StringComparison.Ordinal))
        {
            await HandleUserNotificationAsync(root, cancellationToken);
            return;
        }

        if (string.Equals(kind, RabbitMessageKinds.PasswordResetEmail, StringComparison.Ordinal))
        {
            await HandlePasswordResetEmailAsync(root, cancellationToken);
        }
    }

    private async Task HandleUserNotificationAsync(JsonElement root, CancellationToken cancellationToken)
    {
        if (!root.TryGetProperty("userId", out var userIdEl))
            return;
        var userId = userIdEl.GetString();
        if (string.IsNullOrWhiteSpace(userId))
            return;

        var title = root.TryGetProperty("title", out var tEl) ? tEl.GetString() ?? string.Empty : string.Empty;
        var body = root.TryGetProperty("body", out var bEl) ? bEl.GetString() ?? string.Empty : string.Empty;
        var typeCode = root.TryGetProperty("typeCode", out var cEl) ? cEl.GetString() ?? "generic" : "generic";

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        db.UserNotifications.Add(new UserNotification
        {
            UserId = userId,
            Title = title.Length > 200 ? title[..200] : title,
            Body = body.Length > 2000 ? body[..2000] : body,
            TypeCode = typeCode.Length > 64 ? typeCode[..64] : typeCode,
            CreatedUtc = DateTime.UtcNow,
            ReadAtUtc = null,
        });
        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Persisted user_notification for user {UserId}", userId);
    }

    private async Task HandlePasswordResetEmailAsync(JsonElement root, CancellationToken cancellationToken)
    {
        if (!root.TryGetProperty("email", out var emailEl))
            return;
        var email = emailEl.GetString();
        if (string.IsNullOrWhiteSpace(email))
            return;

        if (!root.TryGetProperty("token", out var tokenEl))
            return;
        var token = tokenEl.GetString();
        if (string.IsNullOrWhiteSpace(token))
            return;

        if (!_emailSender.IsConfigured)
        {
            _logger.LogError("Password reset email for {Email} skipped: SMTP is not configured", email);
            return;
        }

        var subject = "ArenaBook — reset lozinke";
        var body = $"""
            Poštovani,

            Primili ste zahtjev za reset lozinke u aplikaciji ArenaBook.

            Otvorite ekran „Reset lozinke“ u mobilnoj aplikaciji i unesite token:

            {token}

            Ako niste zatražili reset, zanemarite ovu poruku.
            """;

        await _emailSender.SendAsync(email, subject, body, cancellationToken);
        _logger.LogInformation("Sent password reset email to {Email}", email);
    }
}

