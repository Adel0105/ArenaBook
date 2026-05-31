namespace ArenaBook.Application.Contracts.Auth;

public sealed class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

