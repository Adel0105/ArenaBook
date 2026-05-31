namespace ArenaBook.Application.Contracts.Sessions;

public sealed class ScheduledSessionListQuery
{
    public string? Q { get; init; }

    public int? HallId { get; init; }

    public int? SessionKindId { get; init; }

    public int? SessionLifecycleStatusId { get; init; }

    public string? OrganizerUserId { get; init; }

    public string? ParticipantUserId { get; init; }

    public DateTime? DateFromUtc { get; init; }

    public DateTime? DateToUtc { get; init; }
}

