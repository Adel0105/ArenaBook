namespace ArenaBook.Domain.Entities;

public sealed class SessionLifecycleStatus
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public ICollection<ScheduledSession> ScheduledSessions { get; set; } = new List<ScheduledSession>();
}

