namespace ArenaBook.Application.Contracts.Sessions;

public sealed class CreateScheduledSessionRequest
{
    public int HallId { get; set; }

    public int SessionKindId { get; set; }

    public DateTime StartUtc { get; set; }

    public DateTime EndUtc { get; set; }

    public int MaxParticipants { get; set; }

    public int? MaxAgeYears { get; set; }

    public string? InviteCode { get; set; }

    public string? OrganizerUserId { get; set; }
}

