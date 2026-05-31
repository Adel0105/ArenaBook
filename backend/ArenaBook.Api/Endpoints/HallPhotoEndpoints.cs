using ArenaBook.Application.Abstractions.Halls;
using ArenaBook.Application.Authorization;
using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Halls;

namespace ArenaBook.Api.Endpoints;

public static class HallPhotoEndpoints
{
    public static WebApplication MapHallPhotoEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/halls/{hallId:int}/photos")
            .WithTags("Halls - Photos");

        group.MapGet("/", GetPagedAsync)
            .RequireAuthorization(AuthPolicies.CatalogRead);

        group.MapGet("/{photoId:int}", GetByIdAsync)
            .RequireAuthorization(AuthPolicies.CatalogRead);

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(AuthPolicies.AdministratorOnly);

        group.MapPut("/{photoId:int}", UpdateAsync)
            .RequireAuthorization(AuthPolicies.AdministratorOnly);

        group.MapDelete("/{photoId:int}", DeleteAsync)
            .RequireAuthorization(AuthPolicies.AdministratorOnly);

        return app;
    }

    private static Task<PagedListResponse<HallPhotoResponse>> GetPagedAsync(
        IHallPhotoService service,
        int hallId,
        int page = PageRequest.DefaultPage,
        int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        return service.GetPagedAsync(hallId, new PageRequest { Page = page, PageSize = pageSize }, cancellationToken);
    }

    private static Task<HallPhotoResponse> GetByIdAsync(
        IHallPhotoService service,
        int hallId,
        int photoId,
        CancellationToken cancellationToken)
    {
        return service.GetByIdAsync(hallId, photoId, cancellationToken);
    }

    private static Task<HallPhotoResponse> CreateAsync(
        IHallPhotoService service,
        int hallId,
        CreateHallPhotoRequest request,
        CancellationToken cancellationToken)
    {
        return service.CreateAsync(hallId, request, cancellationToken);
    }

    private static Task<HallPhotoResponse> UpdateAsync(
        IHallPhotoService service,
        int hallId,
        int photoId,
        UpdateHallPhotoRequest request,
        CancellationToken cancellationToken)
    {
        return service.UpdateAsync(hallId, photoId, request, cancellationToken);
    }

    private static async Task<IResult> DeleteAsync(
        IHallPhotoService service,
        int hallId,
        int photoId,
        CancellationToken cancellationToken)
    {
        await service.DeleteAsync(hallId, photoId, cancellationToken);
        return Results.NoContent();
    }
}


