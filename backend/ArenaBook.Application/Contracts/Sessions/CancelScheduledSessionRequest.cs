namespace ArenaBook.Application.Contracts.Sessions;

public sealed class CancelScheduledSessionRequest
{
    public string Reason { get; set; } = string.Empty;
}
