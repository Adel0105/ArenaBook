namespace ArenaBook.Application.Abstractions.Notifications;

public interface IUserNotificationPublisher
{
    Task TryPublishAsync(
        string userId,
        string title,
        string body,
        string typeCode,
        CancellationToken cancellationToken = default);

    Task TryPublishManyAsync(
        IEnumerable<UserNotificationMessage> messages,
        CancellationToken cancellationToken = default);
}

public sealed record UserNotificationMessage(
    string UserId,
    string Title,
    string Body,
    string TypeCode);
