using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Reference;

namespace ArenaBook.Application.Abstractions.Reference;

public interface ICityService
{
    Task<PagedListResponse<CityResponse>> GetPagedAsync(
        PageRequest page,
        string? q,
        int? countryId,
        CancellationToken cancellationToken = default);

    Task<CityResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<CityResponse> CreateAsync(CreateCityRequest request, CancellationToken cancellationToken = default);

    Task<CityResponse> UpdateAsync(int id, UpdateCityRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}


