using ArenaBook.Application.Abstractions.Reference;
using ArenaBook.Application.Authorization;
using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Reference;

namespace ArenaBook.Api.Endpoints;

public static class CityEndpoints
{
    public static WebApplication MapCityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/reference/cities")
            .WithTags("Reference - Cities");

        group.MapGet("/", GetPagedAsync)
            .AllowAnonymous();

        group.MapGet("/{id:int}", GetByIdAsync)
            .AllowAnonymous();

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(AuthPolicies.AdministratorOnly);

        group.MapPut("/{id:int}", UpdateAsync)
            .RequireAuthorization(AuthPolicies.AdministratorOnly);

        group.MapDelete("/{id:int}", DeleteAsync)
            .RequireAuthorization(AuthPolicies.AdministratorOnly);

        return app;
    }

    private static Task<PagedListResponse<CityResponse>> GetPagedAsync(
        ICityService service,
        int page = PageRequest.DefaultPage,
        int pageSize = PageRequest.DefaultPageSize,
        string? q = null,
        int? countryId = null,
        CancellationToken cancellationToken = default)
    {
        return service.GetPagedAsync(
            new PageRequest { Page = page, PageSize = pageSize },
            q,
            countryId,
            cancellationToken);
    }

    private static Task<CityResponse> GetByIdAsync(
        ICityService service,
        int id,
        CancellationToken cancellationToken)
    {
        return service.GetByIdAsync(id, cancellationToken);
    }

    private static Task<CityResponse> CreateAsync(
        ICityService service,
        CreateCityRequest request,
        CancellationToken cancellationToken)
    {
        return service.CreateAsync(request, cancellationToken);
    }

    private static Task<CityResponse> UpdateAsync(
        ICityService service,
        int id,
        UpdateCityRequest request,
        CancellationToken cancellationToken)
    {
        return service.UpdateAsync(id, request, cancellationToken);
    }

    private static async Task<IResult> DeleteAsync(
        ICityService service,
        int id,
        CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return Results.NoContent();
    }
}


