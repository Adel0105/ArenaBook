namespace ArenaBook.Application.Contracts.Sessions;

public sealed class UpdateScheduledSessionRequest
{
    public DateTime StartUtc { get; set; }

    public DateTime EndUtc { get; set; }

    public int MaxParticipants { get; set; }

    public int? MaxAgeYears { get; set; }
}

