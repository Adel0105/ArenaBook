namespace ArenaBook.Domain.Entities;

public sealed class ScheduledSessionParticipant
{
    public int Id { get; set; }

    public int ScheduledSessionId { get; set; }

    public ScheduledSession ScheduledSession { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;

    public DateTime JoinedUtc { get; set; }

    public decimal CoinsPaid { get; set; }

    public bool IsOrganizer { get; set; }
}

