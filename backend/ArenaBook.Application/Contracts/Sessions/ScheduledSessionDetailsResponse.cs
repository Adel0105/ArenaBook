namespace ArenaBook.Application.Contracts.Sessions;

public sealed class ScheduledSessionDetailsResponse
{
    public int Id { get; set; }

    public int HallId { get; set; }

    public string HallName { get; set; } = string.Empty;

    public string OrganizerUserId { get; set; } = string.Empty;

    public string? OrganizerEmail { get; set; }

    public int SessionKindId { get; set; }

    public string SessionKindCode { get; set; } = string.Empty;

    public int SessionLifecycleStatusId { get; set; }

    public string SessionLifecycleCode { get; set; } = string.Empty;

    public DateTime StartUtc { get; set; }

    public DateTime EndUtc { get; set; }

    public int MaxParticipants { get; set; }

    public int? MaxAgeYears { get; set; }

    public string? InviteCode { get; set; }

    public DateTime CreatedUtc { get; set; }

    public decimal PriceTotalCoins { get; set; }

    public decimal PricePerParticipantCoins { get; set; }

    public bool IsPricingLocked { get; set; }

    public IReadOnlyList<ScheduledSessionParticipantResponse> Participants { get; set; } = Array.Empty<ScheduledSessionParticipantResponse>();
}

