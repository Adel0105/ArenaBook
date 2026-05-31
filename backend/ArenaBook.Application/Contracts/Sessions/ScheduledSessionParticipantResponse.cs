namespace ArenaBook.Application.Contracts.Sessions;

public sealed class ScheduledSessionParticipantResponse
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string? UserEmail { get; set; }

    public DateTime JoinedUtc { get; set; }

    public decimal CoinsPaid { get; set; }

    public bool IsOrganizer { get; set; }
}

