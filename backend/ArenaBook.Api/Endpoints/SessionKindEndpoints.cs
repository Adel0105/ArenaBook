using ArenaBook.Application.Abstractions.Reference;
using ArenaBook.Application.Authorization;
using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Reference;

namespace ArenaBook.Api.Endpoints;

public static class SessionKindEndpoints
{
    public static WebApplication MapSessionKindEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/reference/session-kinds")
            .WithTags("Reference - SessionKinds");

        group.MapGet("/", GetPagedAsync)
            .RequireAuthorization(AuthPolicies.CatalogRead);

        group.MapGet("/{id:int}", GetByIdAsync)
            .RequireAuthorization(AuthPolicies.CatalogRead);

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(AuthPolicies.AdministratorOnly);

        group.MapPut("/{id:int}", UpdateAsync)
            .RequireAuthorization(AuthPolicies.AdministratorOnly);

        group.MapDelete("/{id:int}", DeleteAsync)
            .RequireAuthorization(AuthPolicies.AdministratorOnly);

        return app;
    }

    private static Task<PagedListResponse<SessionKindResponse>> GetPagedAsync(
        ISessionKindService service,
        int page = PageRequest.DefaultPage,
        int pageSize = PageRequest.DefaultPageSize,
        string? q = null,
        string? code = null,
        CancellationToken cancellationToken = default)
    {
        return service.GetPagedAsync(
            new PageRequest { Page = page, PageSize = pageSize },
            q,
            code,
            cancellationToken);
    }

    private static Task<SessionKindResponse> GetByIdAsync(
        ISessionKindService service,
        int id,
        CancellationToken cancellationToken)
    {
        return service.GetByIdAsync(id, cancellationToken);
    }

    private static Task<SessionKindResponse> CreateAsync(
        ISessionKindService service,
        CreateSessionKindRequest request,
        CancellationToken cancellationToken)
    {
        return service.CreateAsync(request, cancellationToken);
    }

    private static Task<SessionKindResponse> UpdateAsync(
        ISessionKindService service,
        int id,
        UpdateSessionKindRequest request,
        CancellationToken cancellationToken)
    {
        return service.UpdateAsync(id, request, cancellationToken);
    }

    private static async Task<IResult> DeleteAsync(
        ISessionKindService service,
        int id,
        CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return Results.NoContent();
    }
}


