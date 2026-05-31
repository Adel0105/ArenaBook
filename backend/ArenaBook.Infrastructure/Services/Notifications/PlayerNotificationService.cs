using ArenaBook.Application.Abstractions.Notifications;
using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Notifications;
using ArenaBook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArenaBook.Infrastructure.Services.Notifications;

public sealed class PlayerNotificationService : IPlayerNotificationService
{
    private readonly ArenaBookDbContext _db;

    public PlayerNotificationService(ArenaBookDbContext db)
    {
        _db = db;
    }

    public async Task<PagedListResponse<UserNotificationListItemResponse>> GetMyNotificationsPagedAsync(
        string userId,
        PageRequest page,
        bool? unreadOnly,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = page.GetNormalizedPage();
        var normalizedPageSize = page.GetNormalizedPageSize();
        var skip = (normalizedPage - 1) * normalizedPageSize;

        var q = _db.UserNotifications.AsNoTracking().Where(x => x.UserId == userId);
        if (unreadOnly == true)
            q = q.Where(x => x.ReadAtUtc == null);

        var total = await q.CountAsync(cancellationToken);
        var rows = await q
            .OrderByDescending(x => x.CreatedUtc)
            .ThenByDescending(x => x.Id)
            .Skip(skip)
            .Take(normalizedPageSize)
            .Select(x => new UserNotificationListItemResponse
            {
                Id = x.Id,
                Title = x.Title,
                Body = x.Body,
                TypeCode = x.TypeCode,
                CreatedUtc = x.CreatedUtc,
                ReadAtUtc = x.ReadAtUtc,
                IsRead = x.ReadAtUtc != null,
            })
            .ToListAsync(cancellationToken);

        return new PagedListResponse<UserNotificationListItemResponse>
        {
            Items = rows,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = total,
        };
    }

    public async Task MarkReadAsync(string userId, int notificationId, CancellationToken cancellationToken = default)
    {
        var row = await _db.UserNotifications.FirstOrDefaultAsync(
            x => x.Id == notificationId && x.UserId == userId,
            cancellationToken);
        if (row is null)
            return;

        if (row.ReadAtUtc is null)
        {
            row.ReadAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAllReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await _db.UserNotifications
            .Where(x => x.UserId == userId && x.ReadAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ReadAtUtc, _ => now), cancellationToken);
    }
}

