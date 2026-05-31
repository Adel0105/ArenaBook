using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Halls;

namespace ArenaBook.Application.Abstractions.Halls;

public interface IHallPhotoService
{
    Task<PagedListResponse<HallPhotoResponse>> GetPagedAsync(
        int hallId,
        PageRequest page,
        CancellationToken cancellationToken = default);

    Task<HallPhotoResponse> GetByIdAsync(int hallId, int photoId, CancellationToken cancellationToken = default);

    Task<HallPhotoResponse> CreateAsync(int hallId, CreateHallPhotoRequest request, CancellationToken cancellationToken = default);

    Task<HallPhotoResponse> UpdateAsync(int hallId, int photoId, UpdateHallPhotoRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int hallId, int photoId, CancellationToken cancellationToken = default);
}


