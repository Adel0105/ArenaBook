using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Reference;

namespace ArenaBook.Application.Abstractions.Reference;

public interface ISessionLifecycleStatusService
{
    Task<PagedListResponse<SessionLifecycleStatusResponse>> GetPagedAsync(
        PageRequest page,
        string? q,
        string? code,
        CancellationToken cancellationToken = default);

    Task<SessionLifecycleStatusResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<SessionLifecycleStatusResponse> CreateAsync(CreateSessionLifecycleStatusRequest request, CancellationToken cancellationToken = default);

    Task<SessionLifecycleStatusResponse> UpdateAsync(int id, UpdateSessionLifecycleStatusRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}


