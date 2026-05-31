namespace ArenaBook.Domain.Entities;

public sealed class UserNotification
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string TypeCode { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }

    public DateTime? ReadAtUtc { get; set; }
}

