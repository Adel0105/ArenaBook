using System.Text.Json;
using ArenaBook.Application.Abstractions.Messaging;
using ArenaBook.Application.Abstractions.Notifications;
using ArenaBook.Domain.Messaging;
using Microsoft.Extensions.Logging;

namespace ArenaBook.Infrastructure.Services.Notifications;

public sealed class UserNotificationPublisher : IUserNotificationPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IRabbitMqEventPublisher _rabbit;
    private readonly ILogger<UserNotificationPublisher> _logger;

    public UserNotificationPublisher(
        IRabbitMqEventPublisher rabbit,
        ILogger<UserNotificationPublisher> logger)
    {
        _rabbit = rabbit;
        _logger = logger;
    }

    public Task TryPublishAsync(
        string userId,
        string title,
        string body,
        string typeCode,
        CancellationToken cancellationToken = default)
    {
        return TryPublishManyAsync(
            [new UserNotificationMessage(userId, title, body, typeCode)],
            cancellationToken);
    }

    public async Task TryPublishManyAsync(
        IEnumerable<UserNotificationMessage> messages,
        CancellationToken cancellationToken = default)
    {
        foreach (var message in messages)
        {
            if (string.IsNullOrWhiteSpace(message.UserId))
                continue;

            var payload = new
            {
                schemaVersion = 1,
                kind = RabbitMessageKinds.UserNotification,
                userId = message.UserId,
                title = message.Title,
                body = message.Body,
                typeCode = message.TypeCode,
            };

            var json = JsonSerializer.Serialize(payload, JsonOptions);
            try
            {
                await _rabbit.PublishJsonAsync(json, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "RabbitMQ publish notifikacije nije uspio (typeCode={TypeCode}, userId={UserId})",
                    message.TypeCode,
                    message.UserId);
            }
        }
    }
}
