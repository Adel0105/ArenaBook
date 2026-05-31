using ArenaBook.Application.Abstractions.Admin;
using ArenaBook.Application.Authorization;
using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Admin;

namespace ArenaBook.Api.Endpoints;

public static class AdminUserEndpoints
{
    public static WebApplication MapAdminUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/users")
            .RequireAuthorization(AuthPolicies.AdministratorOnly)
            .WithTags("Admin - Users");

        group.MapGet("/", GetPagedAsync);
        group.MapGet("/{userId}", GetByIdAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{userId}", UpdateAsync);
        group.MapPost("/{userId}/lock", LockAsync);
        group.MapPost("/{userId}/unlock", UnlockAsync);

        return app;
    }

    private static Task<PagedListResponse<AdminUserListItemResponse>> GetPagedAsync(
        IAdminUserService service,
        int page = PageRequest.DefaultPage,
        int pageSize = PageRequest.DefaultPageSize,
        string? q = null,
        string? email = null,
        DateTime? registeredFromUtc = null,
        DateTime? registeredToUtc = null,
        bool? isLockedOut = null,
        CancellationToken cancellationToken = default)
    {
        return service.GetPagedAsync(
            new PageRequest { Page = page, PageSize = pageSize },
            new AdminUserListQuery
            {
                Q = q,
                Email = email,
                RegisteredFromUtc = registeredFromUtc,
                RegisteredToUtc = registeredToUtc,
                IsLockedOut = isLockedOut,
            },
            cancellationToken);
    }

    private static Task<AdminUserDetailsResponse> GetByIdAsync(
        IAdminUserService service,
        string userId,
        CancellationToken cancellationToken)
    {
        return service.GetByIdAsync(userId, cancellationToken);
    }

    private static Task<AdminUserDetailsResponse> CreateAsync(
        IAdminUserService service,
        CreateAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        return service.CreateAsync(request, cancellationToken);
    }

    private static Task<AdminUserDetailsResponse> UpdateAsync(
        IAdminUserService service,
        string userId,
        UpdateAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        return service.UpdateAsync(userId, request, cancellationToken);
    }

    private static async Task<IResult> LockAsync(
        IAdminUserService service,
        string userId,
        CancellationToken cancellationToken)
    {
        await service.SetLockedOutAsync(userId, true, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> UnlockAsync(
        IAdminUserService service,
        string userId,
        CancellationToken cancellationToken)
    {
        await service.SetLockedOutAsync(userId, false, cancellationToken);
        return Results.NoContent();
    }
}

