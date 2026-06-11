namespace ArenaBook.Application.Contracts.Auth;

public sealed class PasswordResetResult
{
    public bool EmailDispatched { get; init; }

    public string? DevelopmentToken { get; init; }
}
