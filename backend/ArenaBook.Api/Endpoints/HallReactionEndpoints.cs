using System.Security.Claims;
using ArenaBook.Application.Abstractions.Halls;
using ArenaBook.Application.Authorization;
using ArenaBook.Application.Contracts.Halls;

namespace ArenaBook.Api.Endpoints;

public static class HallReactionEndpoints
{
    public static WebApplication MapHallReactionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/halls/{hallId:int}/reactions")
            .WithTags("Hall Reactions");

        group.MapGet("/", GetSummaryAsync)
            .RequireAuthorization(AuthPolicies.CatalogRead);

        group.MapPut("/", SetReactionAsync)
            .RequireAuthorization(AuthPolicies.PlayerApp);

        return app;
    }

    private static string? TryGetUserId(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrEmpty(id) ? null : id;
    }

    private static string RequireUserId(ClaimsPrincipal user)
    {
        var id = TryGetUserId(user);
        if (string.IsNullOrEmpty(id))
            throw new InvalidOperationException();
        return id;
    }

    private static Task<HallReactionSummaryResponse> GetSummaryAsync(
        ClaimsPrincipal user,
        IHallReactionService service,
        int hallId,
        CancellationToken cancellationToken)
    {
        return service.GetSummaryAsync(hallId, TryGetUserId(user), cancellationToken);
    }

    private static Task<HallReactionSummaryResponse> SetReactionAsync(
        ClaimsPrincipal user,
        IHallReactionService service,
        int hallId,
        SetHallReactionRequest request,
        CancellationToken cancellationToken)
    {
        return service.SetReactionAsync(hallId, RequireUserId(user), request, cancellationToken);
    }
}

