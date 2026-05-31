namespace ArenaBook.Application.Contracts.Halls;

public sealed class CreateHallReviewRequest
{
    public int? ScheduledSessionId { get; set; }

    public byte RatingStars { get; set; }

    public string? Comment { get; set; }
}

