namespace ArenaBook.Application.Contracts.Recommendations;

public sealed class RecommendedSessionResponse
{
    public int SessionId { get; set; }

    public int HallId { get; set; }

    public string HallName { get; set; } = string.Empty;

    public string CityName { get; set; } = string.Empty;

    public string SessionKindCode { get; set; } = string.Empty;

    public DateTime StartUtc { get; set; }

    public DateTime EndUtc { get; set; }

    public int ParticipantCount { get; set; }

    public int MaxParticipants { get; set; }

    public decimal PriceTotalCoins { get; set; }

    public string? OrganizerEmail { get; set; }

    public double Score { get; set; }

    public string Explanation { get; set; } = string.Empty;
}

