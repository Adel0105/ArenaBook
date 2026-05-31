namespace ArenaBook.Application.Contracts.Halls;

public sealed class HallReviewResponse
{
    public int Id { get; set; }

    public int HallId { get; set; }

    public string HallName { get; set; } = string.Empty;

    public int? ScheduledSessionId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string UserDisplayName { get; set; } = string.Empty;

    public byte RatingStars { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedUtc { get; set; }
}

