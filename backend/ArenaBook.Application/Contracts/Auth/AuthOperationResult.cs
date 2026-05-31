namespace ArenaBook.Application.Contracts.Auth;

public sealed class AuthOperationResult
{
    private AuthOperationResult(bool success, AuthTokensResponse? tokens, IReadOnlyList<string>? errors)
    {
        Success = success;
        Tokens = tokens;
        Errors = errors ?? Array.Empty<string>();
    }

    public bool Success { get; }

    public AuthTokensResponse? Tokens { get; }

    public IReadOnlyList<string> Errors { get; }

    public static AuthOperationResult Ok(AuthTokensResponse tokens) =>
        new(true, tokens, null);

    public static AuthOperationResult Fail(params string[] errors) =>
        new(false, null, errors);

    public static AuthOperationResult Fail(IEnumerable<string> errors) =>
        new(false, null, errors.ToArray());
}

