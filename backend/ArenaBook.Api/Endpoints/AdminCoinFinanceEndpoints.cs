using ArenaBook.Application.Abstractions.Coins;
using ArenaBook.Application.Authorization;
using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Coins;

namespace ArenaBook.Api.Endpoints;

public static class AdminCoinFinanceEndpoints
{
    public static WebApplication MapAdminCoinFinanceEndpoints(this WebApplication app)
    {
        var ledger = app.MapGroup("/api/admin/coins/ledger")
            .RequireAuthorization(AuthPolicies.AdministratorOnly)
            .WithTags("Admin - Coins");

        ledger.MapGet("/", GetLedgerAsync);

        var wallets = app.MapGroup("/api/admin/coins/wallets")
            .RequireAuthorization(AuthPolicies.AdministratorOnly)
            .WithTags("Admin - Coins");

        wallets.MapGet("/", GetWalletsAsync);

        var payments = app.MapGroup("/api/admin/external-payments")
            .RequireAuthorization(AuthPolicies.AdministratorOnly)
            .WithTags("Admin - Payments");

        payments.MapGet("/", GetExternalPaymentsAsync);

        app.MapGet("/api/admin/coins/hall-earnings", GetHallEarningsAsync)
            .RequireAuthorization(AuthPolicies.AdministratorOnly)
            .WithTags("Admin - Coins");

        return app;
    }

    private static Task<IReadOnlyList<HallEarningsListItemResponse>> GetHallEarningsAsync(
        IAdminCoinFinanceService service,
        DateTime? dateFromUtc = null,
        DateTime? dateToUtc = null,
        CancellationToken cancellationToken = default)
    {
        return service.GetHallEarningsAsync(dateFromUtc, dateToUtc, cancellationToken);
    }

    private static Task<PagedListResponse<CoinLedgerListItemResponse>> GetLedgerAsync(
        IAdminCoinFinanceService service,
        int page = PageRequest.DefaultPage,
        int pageSize = PageRequest.DefaultPageSize,
        string? userId = null,
        string? reasonCode = null,
        int? relatedScheduledSessionId = null,
        DateTime? dateFromUtc = null,
        DateTime? dateToUtc = null,
        string? q = null,
        bool excludeDemoSeed = false,
        CancellationToken cancellationToken = default)
    {
        return service.GetLedgerPagedAsync(
            new PageRequest { Page = page, PageSize = pageSize },
            new CoinLedgerListQuery
            {
                UserId = userId,
                ReasonCode = reasonCode,
                RelatedScheduledSessionId = relatedScheduledSessionId,
                DateFromUtc = dateFromUtc,
                DateToUtc = dateToUtc,
                Q = q,
                ExcludeDemoSeed = excludeDemoSeed,
            },
            cancellationToken);
    }

    private static Task<PagedListResponse<CoinWalletAdminListItemResponse>> GetWalletsAsync(
        IAdminCoinFinanceService service,
        int page = PageRequest.DefaultPage,
        int pageSize = PageRequest.DefaultPageSize,
        string? q = null,
        CancellationToken cancellationToken = default)
    {
        return service.GetWalletsPagedAsync(
            new PageRequest { Page = page, PageSize = pageSize },
            new CoinWalletListQuery { Q = q },
            cancellationToken);
    }

    private static Task<PagedListResponse<ExternalPaymentAdminListItemResponse>> GetExternalPaymentsAsync(
        IAdminCoinFinanceService service,
        int page = PageRequest.DefaultPage,
        int pageSize = PageRequest.DefaultPageSize,
        string? userId = null,
        int? paymentProcessingStatusId = null,
        string? purposeCode = null,
        string? provider = null,
        DateTime? dateFromUtc = null,
        DateTime? dateToUtc = null,
        string? q = null,
        bool excludeDemoSeed = false,
        CancellationToken cancellationToken = default)
    {
        return service.GetExternalPaymentsPagedAsync(
            new PageRequest { Page = page, PageSize = pageSize },
            new ExternalPaymentListQuery
            {
                UserId = userId,
                PaymentProcessingStatusId = paymentProcessingStatusId,
                PurposeCode = purposeCode,
                Provider = provider,
                DateFromUtc = dateFromUtc,
                DateToUtc = dateToUtc,
                Q = q,
                ExcludeDemoSeed = excludeDemoSeed,
            },
            cancellationToken);
    }
}

