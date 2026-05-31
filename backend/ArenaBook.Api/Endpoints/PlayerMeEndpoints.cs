using System.Security.Claims;
using ArenaBook.Application.Abstractions.Sessions;
using ArenaBook.Application.Authorization;
using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Sessions;

namespace ArenaBook.Api.Endpoints;

public static class PlayerMeEndpoints
{
    public static WebApplication MapPlayerMeEndpoints(this WebApplication app)
    {
        app.MapGet("/api/me/sessions", GetMySessionsAsync)
            .RequireAuthorization(AuthPolicies.PlayerApp)
            .WithTags("Player - Me");

        app.MapGet("/api/me/organized-sessions", GetOrganizedSessionsAsync)
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

    private static Task<PagedListResponse<ScheduledSessionListItemResponse>> GetMySessionsAsync(
        ClaimsPrincipal user,
        IScheduledSessionService service,
        int page = PageRequest.DefaultPage,
        int pageSize = PageRequest.DefaultPageSize,
        int? hallId = null,
        int? sessionLifecycleStatusId = null,
        DateTime? dateFromUtc = null,
        DateTime? dateToUtc = null,
        CancellationToken cancellationToken = default)
    {
        return service.GetPagedAsync(
            new PageRequest { Page = page, PageSize = pageSize },
            new ScheduledSessionListQuery
            {
                HallId = hallId,
                SessionLifecycleStatusId = sessionLifecycleStatusId,
                ParticipantUserId = RequireUserId(user),
                DateFromUtc = dateFromUtc,
                DateToUtc = dateToUtc,
            },
            cancellationToken);
    }

    private static Task<PagedListResponse<ScheduledSessionListItemResponse>> GetOrganizedSessionsAsync(
        ClaimsPrincipal user,
        IScheduledSessionService service,
        int page = PageRequest.DefaultPage,
        int pageSize = PageRequest.DefaultPageSize,
        int? sessionLifecycleStatusId = null,
        CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId(user);
        return service.GetPagedAsync(
            new PageRequest { Page = page, PageSize = pageSize },
            new ScheduledSessionListQuery
            {
                OrganizerUserId = userId,
                SessionLifecycleStatusId = sessionLifecycleStatusId,
            },
            cancellationToken);
    }
}

