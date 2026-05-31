using ArenaBook.Application.Contracts.Halls;

namespace ArenaBook.Application.Abstractions.Halls;

public interface IHallReactionService
{
    Task<HallReactionSummaryResponse> GetSummaryAsync(
        int hallId,
        string? userId,
        CancellationToken cancellationToken = default);

    Task<HallReactionSummaryResponse> SetReactionAsync(
        int hallId,
        string userId,
        SetHallReactionRequest request,
        CancellationToken cancellationToken = default);
}

