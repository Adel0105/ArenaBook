using System.Security.Claims;
using ArenaBook.Application.Abstractions.Notifications;
using ArenaBook.Application.Authorization;
using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Notifications;

namespace ArenaBook.Api.Endpoints;

public static class PlayerNotificationEndpoints
{
    public static WebApplication MapPlayerNotificationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/me/notifications")
            .RequireAuthorization(AuthPolicies.PlayerApp)
            .WithTags("Notifications - Me");

        group.MapGet("/", GetPagedAsync);
        group.MapPost("/{id:int}/read", MarkReadAsync);
        group.MapPost("/read-all", MarkAllReadAsync);

        return app;
    }

    private static string RequireUserId(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id))
            throw new InvalidOperationException();
        return id;
    }

    private static Task<PagedListResponse<UserNotificationListItemResponse>> GetPagedAsync(
        ClaimsPrincipal user,
        IPlayerNotificationService service,
        int page = PageRequest.DefaultPage,
        int pageSize = PageRequest.DefaultPageSize,
        bool? unreadOnly = null,
        CancellationToken cancellationToken = default)
    {
        return service.GetMyNotificationsPagedAsync(
            RequireUserId(user),
            new PageRequest { Page = page, PageSize = pageSize },
            unreadOnly,
            cancellationToken);
    }

    private static async Task<IResult> MarkReadAsync(
        ClaimsPrincipal user,
        IPlayerNotificationService service,
        int id,
        CancellationToken cancellationToken)
    {
        await service.MarkReadAsync(RequireUserId(user), id, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> MarkAllReadAsync(
        ClaimsPrincipal user,
        IPlayerNotificationService service,
        CancellationToken cancellationToken)
    {
        await service.MarkAllReadAsync(RequireUserId(user), cancellationToken);
        return Results.NoContent();
    }
}

