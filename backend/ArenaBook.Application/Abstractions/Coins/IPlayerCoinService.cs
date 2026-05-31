using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Coins;

namespace ArenaBook.Application.Abstractions.Coins;

public interface IPlayerCoinService
{
    Task<CoinWalletResponse> GetMyWalletAsync(string userId, CancellationToken cancellationToken = default);

    Task<PagedListResponse<CoinLedgerListItemResponse>> GetMyLedgerPagedAsync(
        string userId,
        PageRequest page,
        CoinLedgerListQuery query,
        CancellationToken cancellationToken = default);
}

