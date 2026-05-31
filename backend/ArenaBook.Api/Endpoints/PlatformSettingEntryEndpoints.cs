using ArenaBook.Application.Abstractions.Reference;
using ArenaBook.Application.Authorization;
using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Reference;

namespace ArenaBook.Api.Endpoints;

public static class PlatformSettingEntryEndpoints
{
    public static WebApplication MapPlatformSettingEntryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/reference/platform-settings")
            .WithTags("Reference - PlatformSettingEntries");

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

    private static Task<PagedListResponse<PlatformSettingEntryResponse>> GetPagedAsync(
        IPlatformSettingEntryService service,
        int page = PageRequest.DefaultPage,
        int pageSize = PageRequest.DefaultPageSize,
        string? q = null,
        string? key = null,
        CancellationToken cancellationToken = default)
    {
        return service.GetPagedAsync(
            new PageRequest { Page = page, PageSize = pageSize },
            q,
            key,
            cancellationToken);
    }

    private static Task<PlatformSettingEntryResponse> GetByIdAsync(
        IPlatformSettingEntryService service,
        int id,
        CancellationToken cancellationToken)
    {
        return service.GetByIdAsync(id, cancellationToken);
    }

    private static Task<PlatformSettingEntryResponse> CreateAsync(
        IPlatformSettingEntryService service,
        CreatePlatformSettingEntryRequest request,
        CancellationToken cancellationToken)
    {
        return service.CreateAsync(request, cancellationToken);
    }

    private static Task<PlatformSettingEntryResponse> UpdateAsync(
        IPlatformSettingEntryService service,
        int id,
        UpdatePlatformSettingEntryRequest request,
        CancellationToken cancellationToken)
    {
        return service.UpdateAsync(id, request, cancellationToken);
    }

    private static async Task<IResult> DeleteAsync(
        IPlatformSettingEntryService service,
        int id,
        CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return Results.NoContent();
    }
}


