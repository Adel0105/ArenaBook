using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Notifications;

namespace ArenaBook.Application.Abstractions.Notifications;

public interface IPlayerNotificationService
{
    Task<PagedListResponse<UserNotificationListItemResponse>> GetMyNotificationsPagedAsync(
        string userId,
        PageRequest page,
        bool? unreadOnly,
        CancellationToken cancellationToken = default);

    Task MarkReadAsync(string userId, int notificationId, CancellationToken cancellationToken = default);

    Task MarkAllReadAsync(string userId, CancellationToken cancellationToken = default);
}

