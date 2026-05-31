using System.Security.Claims;
using ArenaBook.Application.Abstractions.Recommendations;
using ArenaBook.Application.Authorization;
using ArenaBook.Application.Contracts.Recommendations;

namespace ArenaBook.Api.Endpoints;

public static class RecommendationEndpoints
{
    public static WebApplication MapRecommendationEndpoints(this WebApplication app)
    {
        app.MapGet("/api/me/recommendations/halls", GetRecommendedHallsAsync)
            .RequireAuthorization(AuthPolicies.PlayerApp)
            .WithTags("Recommendations");

        app.MapGet("/api/me/recommendations/sessions", GetRecommendedSessionsAsync)
            .RequireAuthorization(AuthPolicies.PlayerApp)
            .WithTags("Recommendations");

        return app;
    }

    private static string RequireUserId(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id))
            throw new InvalidOperationException();
        return id;
    }

    private static Task<IReadOnlyList<RecommendedHallResponse>> GetRecommendedHallsAsync(
        ClaimsPrincipal user,
        IRecommendationService service,
        int? cityId = null,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        return service.GetRecommendedHallsAsync(RequireUserId(user), cityId, limit, cancellationToken);
    }

    private static Task<IReadOnlyList<RecommendedSessionResponse>> GetRecommendedSessionsAsync(
        ClaimsPrincipal user,
        IRecommendationService service,
        int? cityId = null,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        return service.GetRecommendedSessionsAsync(RequireUserId(user), cityId, limit, cancellationToken);
    }
}

