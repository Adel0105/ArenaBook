using ArenaBook.Application.Abstractions.Reference;
using ArenaBook.Application.Authorization;
using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Reference;

namespace ArenaBook.Api.Endpoints;

public static class EquipmentTypeEndpoints
{
    public static WebApplication MapEquipmentTypeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/reference/equipment-types")
            .WithTags("Reference - EquipmentTypes");

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

    private static Task<PagedListResponse<EquipmentTypeResponse>> GetPagedAsync(
        IEquipmentTypeService service,
        int page = PageRequest.DefaultPage,
        int pageSize = PageRequest.DefaultPageSize,
        string? q = null,
        CancellationToken cancellationToken = default)
    {
        return service.GetPagedAsync(new PageRequest { Page = page, PageSize = pageSize }, q, cancellationToken);
    }

    private static Task<EquipmentTypeResponse> GetByIdAsync(
        IEquipmentTypeService service,
        int id,
        CancellationToken cancellationToken)
    {
        return service.GetByIdAsync(id, cancellationToken);
    }

    private static Task<EquipmentTypeResponse> CreateAsync(
        IEquipmentTypeService service,
        CreateEquipmentTypeRequest request,
        CancellationToken cancellationToken)
    {
        return service.CreateAsync(request, cancellationToken);
    }

    private static Task<EquipmentTypeResponse> UpdateAsync(
        IEquipmentTypeService service,
        int id,
        UpdateEquipmentTypeRequest request,
        CancellationToken cancellationToken)
    {
        return service.UpdateAsync(id, request, cancellationToken);
    }

    private static async Task<IResult> DeleteAsync(
        IEquipmentTypeService service,
        int id,
        CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return Results.NoContent();
    }
}


