using ArenaBook.Application.Abstractions.Halls;
using ArenaBook.Application.Authorization;
using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Halls;

namespace ArenaBook.Api.Endpoints;

public static class HallEndpoints
{
    public static WebApplication MapHallEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/halls")
            .WithTags("Halls");

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

    private static Task<PagedListResponse<HallListItemResponse>> GetPagedAsync(
        IHallService service,
        int page = PageRequest.DefaultPage,
        int pageSize = PageRequest.DefaultPageSize,
        string? q = null,
        int? countryId = null,
        int? cityId = null,
        bool? isActive = null,
        int? minCapacityPeople = null,
        int? maxCapacityPeople = null,
        decimal? minPricePerHourCoins = null,
        decimal? maxPricePerHourCoins = null,
        CancellationToken cancellationToken = default)
    {
        return service.GetPagedAsync(
            new PageRequest { Page = page, PageSize = pageSize },
            new HallListQuery
            {
                Q = q,
                CountryId = countryId,
                CityId = cityId,
                IsActive = isActive,
                MinCapacityPeople = minCapacityPeople,
                MaxCapacityPeople = maxCapacityPeople,
                MinPricePerHourCoins = minPricePerHourCoins,
                MaxPricePerHourCoins = maxPricePerHourCoins,
            },
            cancellationToken);
    }

    private static Task<HallDetailsResponse> GetByIdAsync(
        IHallService service,
        int id,
        CancellationToken cancellationToken)
    {
        return service.GetByIdAsync(id, cancellationToken);
    }

    private static Task<HallDetailsResponse> CreateAsync(
        IHallService service,
        CreateHallRequest request,
        CancellationToken cancellationToken)
    {
        return service.CreateAsync(request, cancellationToken);
    }

    private static Task<HallDetailsResponse> UpdateAsync(
        IHallService service,
        int id,
        UpdateHallRequest request,
        CancellationToken cancellationToken)
    {
        return service.UpdateAsync(id, request, cancellationToken);
    }

    private static async Task<IResult> DeleteAsync(
        IHallService service,
        int id,
        CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return Results.NoContent();
    }
}


