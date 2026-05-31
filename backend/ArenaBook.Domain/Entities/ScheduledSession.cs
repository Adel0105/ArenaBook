namespace ArenaBook.Domain.Entities;

public sealed class ScheduledSession
{
    public int Id { get; set; }

    public int HallId { get; set; }

    public Hall Hall { get; set; } = null!;

    public string OrganizerUserId { get; set; } = string.Empty;

    public int SessionKindId { get; set; }

    public SessionKind SessionKind { get; set; } = null!;

    public int SessionLifecycleStatusId { get; set; }

    public SessionLifecycleStatus SessionLifecycleStatus { get; set; } = null!;

    public DateTime StartUtc { get; set; }

    public DateTime EndUtc { get; set; }

    public int MaxParticipants { get; set; }

    public int? MaxAgeYears { get; set; }

    public string? InviteCode { get; set; }

    public DateTime CreatedUtc { get; set; }

    public ICollection<ScheduledSessionParticipant> Participants { get; set; } = new List<ScheduledSessionParticipant>();

    public ICollection<CoinLedgerEntry> RelatedLedgerEntries { get; set; } = new List<CoinLedgerEntry>();

    public ICollection<HallReview> HallReviews { get; set; } = new List<HallReview>();
}

