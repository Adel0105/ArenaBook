using ArenaBook.Application.Abstractions;
using ArenaBook.Domain.Entities;
using ArenaBook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArenaBook.Infrastructure.Authentication;

public sealed class JwtTokenRevocationService : IJwtTokenRevocationService
{
    private readonly ArenaBookDbContext _db;

    public JwtTokenRevocationService(ArenaBookDbContext db)
    {
        _db = db;
    }

    public async Task RevokeAsync(string jwtId, DateTime expiresUtc, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jwtId))
            return;

        var now = DateTime.UtcNow;
        if (expiresUtc <= now)
            return;

        if (await _db.RevokedAccessTokens.AsNoTracking()
                .AnyAsync(x => x.JwtId == jwtId, cancellationToken))
            return;

        _db.RevokedAccessTokens.Add(new RevokedAccessToken
        {
            JwtId = jwtId,
            ExpiresUtc = expiresUtc,
            RevokedUtc = now,
        });

        await _db.RevokedAccessTokens
            .Where(x => x.ExpiresUtc <= now)
            .ExecuteDeleteAsync(cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> IsRevokedAsync(string jwtId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jwtId))
            return Task.FromResult(false);

        return _db.RevokedAccessTokens.AsNoTracking()
            .AnyAsync(x => x.JwtId == jwtId, cancellationToken);
    }
}
