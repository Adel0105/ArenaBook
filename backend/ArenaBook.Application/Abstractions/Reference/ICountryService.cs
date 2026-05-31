using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Reference;

namespace ArenaBook.Application.Abstractions.Reference;

public interface ICountryService
{
    Task<PagedListResponse<CountryResponse>> GetPagedAsync(PageRequest page, string? q, CancellationToken cancellationToken = default);

    Task<CountryResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<CountryResponse> CreateAsync(CreateCountryRequest request, CancellationToken cancellationToken = default);

    Task<CountryResponse> UpdateAsync(int id, UpdateCountryRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}


