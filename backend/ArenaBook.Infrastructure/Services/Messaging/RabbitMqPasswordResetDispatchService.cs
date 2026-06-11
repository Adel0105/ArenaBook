using System.Text.Json;
using ArenaBook.Application.Abstractions.Messaging;
using ArenaBook.Application.Options;
using ArenaBook.Domain.Messaging;
using Microsoft.Extensions.Options;

namespace ArenaBook.Infrastructure.Services.Messaging;

public sealed class RabbitMqPasswordResetDispatchService : IPasswordResetDispatchService
{
    private readonly IRabbitMqEventPublisher _rabbit;
    private readonly RabbitMqOptions _rabbitOptions;
    private readonly SmtpOptions _smtpOptions;

    public RabbitMqPasswordResetDispatchService(
        IRabbitMqEventPublisher rabbit,
        IOptions<RabbitMqOptions> rabbitOptions,
        IOptions<SmtpOptions> smtpOptions)
    {
        _rabbit = rabbit;
        _rabbitOptions = rabbitOptions.Value;
        _smtpOptions = smtpOptions.Value;
    }

    public bool IsAvailable =>
        !string.IsNullOrWhiteSpace(_rabbitOptions.Host) && _smtpOptions.IsConfigured;

    public async Task DispatchAsync(string email, string resetToken, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("Reset lozinke preko e-maila nije konfiguriran (RabbitMQ + SMTP).");

        var payload = new
        {
            schemaVersion = 1,
            kind = RabbitMessageKinds.PasswordResetEmail,
            email,
            token = resetToken,
        };
        var json = JsonSerializer.Serialize(
            payload,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await _rabbit.PublishJsonAsync(json, cancellationToken);
    }
}
