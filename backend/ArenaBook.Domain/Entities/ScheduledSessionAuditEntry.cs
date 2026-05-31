namespace ArenaBook.Domain.Entities;

public sealed class ScheduledSessionAuditEntry
{
    public long Id { get; set; }

    public int SessionId { get; set; }

    public string ActorUserId { get; set; } = string.Empty;

    public DateTime OccurredUtc { get; set; }

    public string Action { get; set; } = string.Empty;

    public int? FromLifecycleStatusId { get; set; }

    public int? ToLifecycleStatusId { get; set; }

    public string? DetailsJson { get; set; }
}

