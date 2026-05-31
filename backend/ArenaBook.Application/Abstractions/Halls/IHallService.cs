using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Halls;

namespace ArenaBook.Application.Abstractions.Halls;

public interface IHallService
{
    Task<PagedListResponse<HallListItemResponse>> GetPagedAsync(
        PageRequest page,
        HallListQuery query,
        CancellationToken cancellationToken = default);

    Task<HallDetailsResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<HallDetailsResponse> CreateAsync(CreateHallRequest request, CancellationToken cancellationToken = default);

    Task<HallDetailsResponse> UpdateAsync(int id, UpdateHallRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}


