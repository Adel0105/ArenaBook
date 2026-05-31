using ArenaBook.Application.Abstractions.Admin;
using ArenaBook.Application.Authorization;
using ArenaBook.Application.Contracts.Admin;

namespace ArenaBook.Api.Endpoints;

public static class AdminDashboardEndpoints
{
    public static WebApplication MapAdminDashboardEndpoints(this WebApplication app)
    {
        app.MapGet("/api/admin/dashboard/summary", GetSummaryAsync)
            .RequireAuthorization(AuthPolicies.AdministratorOnly)
            .WithTags("Admin - Dashboard");

        app.MapGet("/api/admin/dashboard/activity", GetActivityAsync)
            .RequireAuthorization(AuthPolicies.AdministratorOnly)
            .WithTags("Admin - Dashboard");

        return app;
    }

    private static Task<AdminDashboardSummaryResponse> GetSummaryAsync(
        IAdminDashboardService service,
        CancellationToken cancellationToken)
    {
        return service.GetSummaryAsync(cancellationToken);
    }

    private static Task<AdminDashboardActivityResponse> GetActivityAsync(
        IAdminDashboardService service,
        int months = 6,
        CancellationToken cancellationToken = default)
    {
        return service.GetActivityAsync(months, cancellationToken);
    }
}

