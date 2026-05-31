using System.Security.Claims;
using ArenaBook.Application.Abstractions.Coins;
using ArenaBook.Application.Authorization;
using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Coins;

namespace ArenaBook.Api.Endpoints;

public static class PlayerCoinEndpoints
{
    public static WebApplication MapPlayerCoinEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/me/coins")
            .RequireAuthorization(AuthPolicies.PlayerApp)
            .WithTags("Coins - Me");

        group.MapGet("/wallet", GetWalletAsync);
        group.MapGet("/ledger", GetLedgerAsync);

        return app;
    }

    private static string RequireUserId(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id))
            throw new InvalidOperationException();
        return id;
    }

    private static Task<CoinWalletResponse> GetWalletAsync(
        ClaimsPrincipal user,
        IPlayerCoinService service,
        CancellationToken cancellationToken)
    {
        return service.GetMyWalletAsync(RequireUserId(user), cancellationToken);
    }

    private static Task<PagedListResponse<CoinLedgerListItemResponse>> GetLedgerAsync(
        ClaimsPrincipal user,
        IPlayerCoinService service,
        int page = PageRequest.DefaultPage,
        int pageSize = PageRequest.DefaultPageSize,
        string? reasonCode = null,
        int? relatedScheduledSessionId = null,
        DateTime? dateFromUtc = null,
        DateTime? dateToUtc = null,
        CancellationToken cancellationToken = default)
    {
        return service.GetMyLedgerPagedAsync(
            RequireUserId(user),
            new PageRequest { Page = page, PageSize = pageSize },
            new CoinLedgerListQuery
            {
                ReasonCode = reasonCode,
                RelatedScheduledSessionId = relatedScheduledSessionId,
                DateFromUtc = dateFromUtc,
                DateToUtc = dateToUtc,
            },
            cancellationToken);
    }
}

