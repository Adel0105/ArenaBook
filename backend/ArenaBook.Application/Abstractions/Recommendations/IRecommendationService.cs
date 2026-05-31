using ArenaBook.Application.Contracts.Recommendations;

namespace ArenaBook.Application.Abstractions.Recommendations;

public interface IRecommendationService
{
    Task<IReadOnlyList<RecommendedHallResponse>> GetRecommendedHallsAsync(
        string userId,
        int? cityId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecommendedSessionResponse>> GetRecommendedSessionsAsync(
        string userId,
        int? cityId,
        int limit,
        CancellationToken cancellationToken = default);
}

