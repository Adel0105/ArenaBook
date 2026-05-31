using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Reference;

namespace ArenaBook.Application.Abstractions.Reference;

public interface IPlatformSettingEntryService
{
    Task<PagedListResponse<PlatformSettingEntryResponse>> GetPagedAsync(
        PageRequest page,
        string? q,
        string? key,
        CancellationToken cancellationToken = default);

    Task<PlatformSettingEntryResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PlatformSettingEntryResponse> CreateAsync(CreatePlatformSettingEntryRequest request, CancellationToken cancellationToken = default);

    Task<PlatformSettingEntryResponse> UpdateAsync(int id, UpdatePlatformSettingEntryRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}


