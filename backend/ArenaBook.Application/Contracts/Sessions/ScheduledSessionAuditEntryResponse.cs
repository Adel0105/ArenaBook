namespace ArenaBook.Application.Contracts.Sessions;

public sealed class ScheduledSessionAuditEntryResponse
{
    public long Id { get; set; }

    public int SessionId { get; set; }

    public string ActorUserId { get; set; } = string.Empty;

    public string? ActorEmail { get; set; }

    public DateTime OccurredUtc { get; set; }

    public string Action { get; set; } = string.Empty;

    public int? FromLifecycleStatusId { get; set; }

    public string? FromLifecycleCode { get; set; }

    public int? ToLifecycleStatusId { get; set; }

    public string? ToLifecycleCode { get; set; }

    public string? DetailsJson { get; set; }
}

