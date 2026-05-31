using System.Security.Claims;
using ArenaBook.Application.Abstractions;
using ArenaBook.Application.Authorization;

namespace ArenaBook.Api.Endpoints;

public static class PlayerProfileEndpoints
{
    public static WebApplication MapPlayerProfileEndpoints(this WebApplication app)
    {
        app.MapGet("/api/me/profile-stats", GetStatsAsync)
            .RequireAuthorization(AuthPolicies.PlayerApp)
            .WithTags("Player - Me");

        return app;
    }

    private static string RequireUserId(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id))
            throw new InvalidOperationException();
        return id;
    }

    private static Task<Application.Contracts.Auth.PlayerProfileStatsResponse> GetStatsAsync(
        ClaimsPrincipal user,
        IPlayerProfileService service,
        CancellationToken cancellationToken)
    {
        return service.GetStatsAsync(RequireUserId(user), cancellationToken);
    }
}

