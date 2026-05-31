using ArenaBook.Application.Abstractions.Halls;
using ArenaBook.Application.Authorization;
using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Halls;

namespace ArenaBook.Api.Endpoints;

public static class HallEquipmentEndpoints
{
    public static WebApplication MapHallEquipmentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/halls/{hallId:int}/equipment")
            .WithTags("Halls - Equipment");

        group.MapGet("/", GetPagedAsync)
            .RequireAuthorization(AuthPolicies.CatalogRead);

        group.MapGet("/{linkId:int}", GetByIdAsync)
            .RequireAuthorization(AuthPolicies.CatalogRead);

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(AuthPolicies.AdministratorOnly);

        group.MapPut("/{linkId:int}", UpdateAsync)
            .RequireAuthorization(AuthPolicies.AdministratorOnly);

        group.MapDelete("/{linkId:int}", DeleteAsync)
            .RequireAuthorization(AuthPolicies.AdministratorOnly);

        return app;
    }

    private static Task<PagedListResponse<HallEquipmentResponse>> GetPagedAsync(
        IHallEquipmentService service,
        int hallId,
        int page = PageRequest.DefaultPage,
        int pageSize = PageRequest.DefaultPageSize,
        int? equipmentTypeId = null,
        CancellationToken cancellationToken = default)
    {
        return service.GetPagedAsync(
            hallId,
            new PageRequest { Page = page, PageSize = pageSize },
            equipmentTypeId,
            cancellationToken);
    }

    private static Task<HallEquipmentResponse> GetByIdAsync(
        IHallEquipmentService service,
        int hallId,
        int linkId,
        CancellationToken cancellationToken)
    {
        return service.GetByIdAsync(hallId, linkId, cancellationToken);
    }

    private static Task<HallEquipmentResponse> CreateAsync(
        IHallEquipmentService service,
        int hallId,
        CreateHallEquipmentRequest request,
        CancellationToken cancellationToken)
    {
        return service.CreateAsync(hallId, request, cancellationToken);
    }

    private static Task<HallEquipmentResponse> UpdateAsync(
        IHallEquipmentService service,
        int hallId,
        int linkId,
        UpdateHallEquipmentRequest request,
        CancellationToken cancellationToken)
    {
        return service.UpdateAsync(hallId, linkId, request, cancellationToken);
    }

    private static async Task<IResult> DeleteAsync(
        IHallEquipmentService service,
        int hallId,
        int linkId,
        CancellationToken cancellationToken)
    {
        await service.DeleteAsync(hallId, linkId, cancellationToken);
        return Results.NoContent();
    }
}


