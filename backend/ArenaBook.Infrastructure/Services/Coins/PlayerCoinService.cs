using ArenaBook.Application.Abstractions.Coins;
using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Coins;
using ArenaBook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArenaBook.Infrastructure.Services.Coins;

public sealed class PlayerCoinService : IPlayerCoinService
{
    private readonly ArenaBookDbContext _db;

    public PlayerCoinService(ArenaBookDbContext db)
    {
        _db = db;
    }

    public async Task<CoinWalletResponse> GetMyWalletAsync(string userId, CancellationToken cancellationToken = default)
    {
        var row = await _db.UserCoinWallets.AsNoTracking()
            .Where(w => w.UserId == userId)
            .Select(w => new { w.BalanceCoins, w.UpdatedUtc })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return new CoinWalletResponse
            {
                UserId = userId,
                BalanceCoins = 0,
                UpdatedUtc = null,
                WalletExists = false,
            };
        }

        return new CoinWalletResponse
        {
            UserId = userId,
            BalanceCoins = row.BalanceCoins,
            UpdatedUtc = row.UpdatedUtc,
            WalletExists = true,
        };
    }

    public async Task<PagedListResponse<CoinLedgerListItemResponse>> GetMyLedgerPagedAsync(
        string userId,
        PageRequest page,
        CoinLedgerListQuery query,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = page.GetNormalizedPage();
        var normalizedPageSize = page.GetNormalizedPageSize();
        var skip = (normalizedPage - 1) * normalizedPageSize;

        var walletId = await _db.UserCoinWallets.AsNoTracking()
            .Where(w => w.UserId == userId)
            .Select(w => (int?)w.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (walletId is null)
        {
            return new PagedListResponse<CoinLedgerListItemResponse>
            {
                Items = Array.Empty<CoinLedgerListItemResponse>(),
                Page = normalizedPage,
                PageSize = normalizedPageSize,
                TotalCount = 0,
            };
        }

        var sessionsWithHalls = _db.ScheduledSessions.AsNoTracking()
            .Select(s => new { s.Id, HallName = s.Hall.Name, s.StartUtc });

        var baseQ =
            from e in _db.CoinLedgerEntries.AsNoTracking()
            where e.UserCoinWalletId == walletId.Value
            join sh in sessionsWithHalls on e.RelatedScheduledSessionId equals sh.Id into shg
            from sh in shg.DefaultIfEmpty()
            select new { e, RelatedHallName = sh != null ? sh.HallName : null, RelatedSessionStartUtc = sh != null ? sh.StartUtc : (DateTime?)null };

        if (!string.IsNullOrWhiteSpace(query.ReasonCode))
        {
            var rc = query.ReasonCode.Trim();
            baseQ = baseQ.Where(x => x.e.ReasonCode == rc);
        }

        if (query.RelatedScheduledSessionId.HasValue)
            baseQ = baseQ.Where(x => x.e.RelatedScheduledSessionId == query.RelatedScheduledSessionId.Value);

        if (query.DateFromUtc.HasValue)
            baseQ = baseQ.Where(x => x.e.CreatedUtc >= query.DateFromUtc.Value);

        if (query.DateToUtc.HasValue)
            baseQ = baseQ.Where(x => x.e.CreatedUtc <= query.DateToUtc.Value);

        var total = await baseQ.CountAsync(cancellationToken);
        var rows = await baseQ
            .OrderByDescending(x => x.e.CreatedUtc)
            .ThenByDescending(x => x.e.Id)
            .Skip(skip)
            .Take(normalizedPageSize)
            .Select(x => new CoinLedgerListItemResponse
            {
                Id = x.e.Id,
                UserId = userId,
                UserEmail = null,
                AmountCoins = x.e.AmountCoins,
                BalanceAfter = x.e.BalanceAfter,
                ReasonCode = x.e.ReasonCode,
                RelatedScheduledSessionId = x.e.RelatedScheduledSessionId,
                RelatedHallName = x.RelatedHallName,
                RelatedSessionStartUtc = x.RelatedSessionStartUtc,
                CreatedUtc = x.e.CreatedUtc,
            })
            .ToListAsync(cancellationToken);

        return new PagedListResponse<CoinLedgerListItemResponse>
        {
            Items = rows,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = total,
        };
    }
}

