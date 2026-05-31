using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Reference;

namespace ArenaBook.Application.Abstractions.Reference;

public interface ISessionKindService
{
    Task<PagedListResponse<SessionKindResponse>> GetPagedAsync(PageRequest page, string? q, string? code, CancellationToken cancellationToken = default);

    Task<SessionKindResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<SessionKindResponse> CreateAsync(CreateSessionKindRequest request, CancellationToken cancellationToken = default);

    Task<SessionKindResponse> UpdateAsync(int id, UpdateSessionKindRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}


