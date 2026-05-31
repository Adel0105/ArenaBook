namespace ArenaBook.Application.Contracts.Auth;

public sealed class AuthTokensResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public string TokenType { get; set; } = "Bearer";

    public int ExpiresInSeconds { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }
}

