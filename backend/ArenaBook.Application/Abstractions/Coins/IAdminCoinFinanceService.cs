using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Coins;

namespace ArenaBook.Application.Abstractions.Coins;

public interface IAdminCoinFinanceService
{
    Task<PagedListResponse<CoinLedgerListItemResponse>> GetLedgerPagedAsync(
        PageRequest page,
        CoinLedgerListQuery query,
        CancellationToken cancellationToken = default);

    Task<PagedListResponse<CoinWalletAdminListItemResponse>> GetWalletsPagedAsync(
        PageRequest page,
        CoinWalletListQuery query,
        CancellationToken cancellationToken = default);

    Task<PagedListResponse<ExternalPaymentAdminListItemResponse>> GetExternalPaymentsPagedAsync(
        PageRequest page,
        ExternalPaymentListQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HallEarningsListItemResponse>> GetHallEarningsAsync(
        DateTime? dateFromUtc,
        DateTime? dateToUtc,
        CancellationToken cancellationToken = default);
}

