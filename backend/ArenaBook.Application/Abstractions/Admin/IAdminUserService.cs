using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Admin;

namespace ArenaBook.Application.Abstractions.Admin;

public interface IAdminUserService
{
    Task<PagedListResponse<AdminUserListItemResponse>> GetPagedAsync(
        PageRequest page,
        AdminUserListQuery query,
        CancellationToken cancellationToken = default);

    Task<AdminUserDetailsResponse> GetByIdAsync(string userId, CancellationToken cancellationToken = default);

    Task<AdminUserDetailsResponse> CreateAsync(CreateAdminUserRequest request, CancellationToken cancellationToken = default);

    Task<AdminUserDetailsResponse> UpdateAsync(string userId, UpdateAdminUserRequest request, CancellationToken cancellationToken = default);

    Task SetLockedOutAsync(string userId, bool lockedOut, CancellationToken cancellationToken = default);
}

