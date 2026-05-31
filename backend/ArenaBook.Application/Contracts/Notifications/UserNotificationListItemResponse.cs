namespace ArenaBook.Application.Contracts.Notifications;

public sealed class UserNotificationListItemResponse
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string TypeCode { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }

    public DateTime? ReadAtUtc { get; set; }

    public bool IsRead { get; set; }
}

