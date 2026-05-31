using ArenaBook.Application.Contracts.Auth;

namespace ArenaBook.Application.Abstractions;

public interface IPlayerProfileService
{
    Task<PlayerProfileStatsResponse> GetStatsAsync(string userId, CancellationToken cancellationToken = default);
}

