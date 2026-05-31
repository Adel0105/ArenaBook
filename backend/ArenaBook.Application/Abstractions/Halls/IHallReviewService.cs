using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Halls;

namespace ArenaBook.Application.Abstractions.Halls;

public interface IHallReviewService
{
    Task<PagedListResponse<HallReviewResponse>> GetByHallIdAsync(
        int hallId,
        PageRequest page,
        CancellationToken cancellationToken = default);

    Task<HallReviewResponse> CreateAsync(
        int hallId,
        string userId,
        CreateHallReviewRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HallReviewResponse>> GetPendingForUserAsync(
        string userId,
        CancellationToken cancellationToken = default);
}

