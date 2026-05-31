namespace ArenaBook.Domain.Entities;

public sealed class HallReview
{
    public int Id { get; set; }

    public int HallId { get; set; }

    public Hall Hall { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;

    public int? ScheduledSessionId { get; set; }

    public ScheduledSession? ScheduledSession { get; set; }

    public byte RatingStars { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedUtc { get; set; }
}

