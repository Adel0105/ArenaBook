namespace ArenaBook.Application.Contracts.Auth;

public sealed class PlayerProfileStatsResponse
{
    public int TotalParticipations { get; set; }

    public int CompletedParticipations { get; set; }

    public int OrganizedSessions { get; set; }

    public int UpcomingParticipations { get; set; }

    public decimal TotalCoinsSpentOnSessions { get; set; }

    public decimal TotalCoinsPurchased { get; set; }

    public double PlayFrequencyPerMonth { get; set; }
}

