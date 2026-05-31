namespace ArenaBook.Application.Abstractions;

public interface IJwtTokenRevocationService
{
    Task RevokeAsync(string jwtId, DateTime expiresUtc, CancellationToken cancellationToken = default);

    Task<bool> IsRevokedAsync(string jwtId, CancellationToken cancellationToken = default);
}
