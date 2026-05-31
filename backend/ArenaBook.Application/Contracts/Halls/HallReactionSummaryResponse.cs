namespace ArenaBook.Application.Contracts.Halls;

public sealed class HallReactionSummaryResponse
{
    public int LikeCount { get; set; }

    public int DislikeCount { get; set; }

    public string? UserReaction { get; set; }
}

