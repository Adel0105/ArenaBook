using System.Security.Claims;
using ArenaBook.Application.Abstractions.Halls;
using ArenaBook.Application.Authorization;
using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Halls;

namespace ArenaBook.Api.Endpoints;

public static class HallReviewEndpoints
{
    public static WebApplication MapHallReviewEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/halls/{hallId:int}/reviews")
            .WithTags("Hall Reviews");

        group.MapGet("/", GetPagedAsync)
            .RequireAuthorization(AuthPolicies.CatalogRead);

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(AuthPolicies.PlayerApp);

        app.MapGet("/api/me/reviews/pending", GetPendingAsync)
            .RequireAuthorization(AuthPolicies.PlayerApp)
            .WithTags("Hall Reviews");

        return app;
    }

    private static string RequireUserId(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id))
            throw new InvalidOperationException();
        return id;
    }

    private static Task<PagedListResponse<HallReviewResponse>> GetPagedAsync(
        IHallReviewService service,
        int hallId,
        int page = PageRequest.DefaultPage,
        int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        return service.GetByHallIdAsync(hallId, new PageRequest { Page = page, PageSize = pageSize }, cancellationToken);
    }

    private static Task<HallReviewResponse> CreateAsync(
        ClaimsPrincipal user,
        IHallReviewService service,
        int hallId,
        CreateHallReviewRequest request,
        CancellationToken cancellationToken)
    {
        return service.CreateAsync(hallId, RequireUserId(user), request, cancellationToken);
    }

    private static Task<IReadOnlyList<HallReviewResponse>> GetPendingAsync(
        ClaimsPrincipal user,
        IHallReviewService service,
        CancellationToken cancellationToken)
    {
        return service.GetPendingForUserAsync(RequireUserId(user), cancellationToken);
    }
}

