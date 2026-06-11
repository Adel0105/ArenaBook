using System.Security.Claims;
using ArenaBook.Application.Abstractions.Sessions;
using ArenaBook.Application.Authorization;
using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Sessions;
using ArenaBook.Domain.Security;

namespace ArenaBook.Api.Endpoints;

public static class ScheduledSessionEndpoints
{
    public static WebApplication MapScheduledSessionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/sessions")
            .WithTags("Sessions");

        group.MapGet("/", GetPagedAsync)
            .RequireAuthorization(AuthPolicies.CatalogRead);

        group.MapGet("/{id:int}", GetByIdAsync)
            .RequireAuthorization(AuthPolicies.CatalogRead);

        group.MapGet("/{id:int}/join-coins", GetJoinCoinQuoteAsync)
            .RequireAuthorization(AuthPolicies.CatalogRead);

        group.MapGet("/{id:int}/audit", GetAuditTrailAsync)
            .RequireAuthorization();

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(p => p.RequireRole(
                ApplicationRoles.Administrator,
                ApplicationRoles.Member,
                ApplicationRoles.Organizer));

        group.MapPut("/{id:int}", UpdateAsync)
            .RequireAuthorization();

        group.MapPost("/{id:int}/confirm", ConfirmAsync)
            .RequireAuthorization();

        group.MapPost("/{id:int}/cancel", CancelAsync)
            .RequireAuthorization();

        group.MapPost("/{id:int}/complete", CompleteAsync)
            .RequireAuthorization();

        group.MapPost("/{id:int}/join", JoinAsync)
            .RequireAuthorization(AuthPolicies.PlayerApp);

        group.MapDelete("/{id:int}", DeleteAsync)
            .RequireAuthorization(AuthPolicies.AdministratorOnly);

        return app;
    }

    private static string RequireUserId(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id))
            throw new InvalidOperationException();
        return id;
    }

    private static bool IsAdministrator(ClaimsPrincipal user) =>
        user.IsInRole(ApplicationRoles.Administrator);

    private static Task<PagedListResponse<ScheduledSessionListItemResponse>> GetPagedAsync(
        IScheduledSessionService service,
        int page = PageRequest.DefaultPage,
        int pageSize = PageRequest.DefaultPageSize,
        string? q = null,
        int? hallId = null,
        int? sessionKindId = null,
        int? sessionLifecycleStatusId = null,
        string? organizerUserId = null,
        string? participantUserId = null,
        DateTime? dateFromUtc = null,
        DateTime? dateToUtc = null,
        CancellationToken cancellationToken = default)
    {
        return service.GetPagedAsync(
            new PageRequest { Page = page, PageSize = pageSize },
            new ScheduledSessionListQuery
            {
                Q = q,
                HallId = hallId,
                SessionKindId = sessionKindId,
                SessionLifecycleStatusId = sessionLifecycleStatusId,
                OrganizerUserId = organizerUserId,
                ParticipantUserId = participantUserId,
                DateFromUtc = dateFromUtc,
                DateToUtc = dateToUtc,
            },
            cancellationToken);
    }

    private static Task<ScheduledSessionDetailsResponse> GetByIdAsync(
        ClaimsPrincipal user,
        IScheduledSessionService service,
        int id,
        CancellationToken cancellationToken)
    {
        return service.GetByIdAsync(
            id,
            RequireUserId(user),
            IsAdministrator(user),
            cancellationToken);
    }

    private static Task<SessionJoinCoinQuoteResponse> GetJoinCoinQuoteAsync(
        IScheduledSessionService service,
        int id,
        CancellationToken cancellationToken)
    {
        return service.GetJoinCoinQuoteAsync(id, cancellationToken);
    }

    private static Task<IReadOnlyList<ScheduledSessionAuditEntryResponse>> GetAuditTrailAsync(
        ClaimsPrincipal user,
        IScheduledSessionService service,
        int id,
        CancellationToken cancellationToken)
    {
        return service.GetAuditTrailAsync(id, RequireUserId(user), IsAdministrator(user), cancellationToken);
    }

    private static Task<ScheduledSessionDetailsResponse> CreateAsync(
        ClaimsPrincipal user,
        IScheduledSessionService service,
        CreateScheduledSessionRequest body,
        CancellationToken cancellationToken)
    {
        var organizerId = IsAdministrator(user) && !string.IsNullOrWhiteSpace(body.OrganizerUserId)
            ? body.OrganizerUserId!.Trim()
            : RequireUserId(user);
        return service.CreateAsync(body, organizerId, cancellationToken);
    }

    private static Task<ScheduledSessionDetailsResponse> UpdateAsync(
        ClaimsPrincipal user,
        IScheduledSessionService service,
        int id,
        UpdateScheduledSessionRequest body,
        CancellationToken cancellationToken)
    {
        return service.UpdateAsync(id, body, RequireUserId(user), IsAdministrator(user), cancellationToken);
    }

    private static Task<ScheduledSessionDetailsResponse> ConfirmAsync(
        ClaimsPrincipal user,
        IScheduledSessionService service,
        int id,
        CancellationToken cancellationToken)
    {
        return service.ConfirmAsync(id, RequireUserId(user), IsAdministrator(user), cancellationToken);
    }

    private static Task<ScheduledSessionDetailsResponse> CancelAsync(
        ClaimsPrincipal user,
        IScheduledSessionService service,
        int id,
        CancelScheduledSessionRequest body,
        CancellationToken cancellationToken)
    {
        return service.CancelAsync(id, body, RequireUserId(user), IsAdministrator(user), cancellationToken);
    }

    private static Task<ScheduledSessionDetailsResponse> CompleteAsync(
        ClaimsPrincipal user,
        IScheduledSessionService service,
        int id,
        CancellationToken cancellationToken)
    {
        return service.CompleteAsync(id, RequireUserId(user), IsAdministrator(user), cancellationToken);
    }

    private static async Task<IResult> JoinAsync(
        ClaimsPrincipal user,
        IScheduledSessionService service,
        int id,
        JoinScheduledSessionRequest body,
        CancellationToken cancellationToken)
    {
        await service.JoinAsync(id, body, RequireUserId(user), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteAsync(
        ClaimsPrincipal user,
        IScheduledSessionService service,
        int id,
        CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, RequireUserId(user), cancellationToken);
        return Results.NoContent();
    }
}

