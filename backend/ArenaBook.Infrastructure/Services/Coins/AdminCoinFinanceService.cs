using ArenaBook.Application.Abstractions.Coins;
using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Coins;
using ArenaBook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArenaBook.Infrastructure.Services.Coins;

public sealed class AdminCoinFinanceService : IAdminCoinFinanceService
{
    private readonly ArenaBookDbContext _db;

    public AdminCoinFinanceService(ArenaBookDbContext db)
    {
        _db = db;
    }

    public async Task<PagedListResponse<CoinLedgerListItemResponse>> GetLedgerPagedAsync(
        PageRequest page,
        CoinLedgerListQuery query,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = page.GetNormalizedPage();
        var normalizedPageSize = page.GetNormalizedPageSize();
        var skip = (normalizedPage - 1) * normalizedPageSize;

        var sessionsWithHalls = _db.ScheduledSessions.AsNoTracking()
            .Select(s => new { s.Id, HallName = s.Hall.Name, s.StartUtc });

        var baseQ =
            from e in _db.CoinLedgerEntries.AsNoTracking()
            join w in _db.UserCoinWallets.AsNoTracking() on e.UserCoinWalletId equals w.Id
            join u in _db.Users.AsNoTracking() on w.UserId equals u.Id
            join sh in sessionsWithHalls on e.RelatedScheduledSessionId equals sh.Id into shg
            from sh in shg.DefaultIfEmpty()
            select new
            {
                e,
                w.UserId,
                u.Email,
                RelatedHallName = sh != null ? sh.HallName : null,
                RelatedSessionStartUtc = sh != null ? sh.StartUtc : (DateTime?)null,
            };

        if (!string.IsNullOrWhiteSpace(query.UserId))
            baseQ = baseQ.Where(x => x.UserId == query.UserId);

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

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = query.Q.Trim();
            baseQ = baseQ.Where(x => x.Email != null && x.Email.Contains(term));
        }

        if (query.ExcludeDemoSeed)
            baseQ = baseQ.Where(x => x.e.ReasonCode != Domain.CoinLedgerReasonCodes.SeedInitial);

        var total = await baseQ.CountAsync(cancellationToken);
        var rows = await baseQ
            .OrderBy(x => x.e.ReasonCode == Domain.CoinLedgerReasonCodes.SeedInitial)
            .ThenByDescending(x => x.e.CreatedUtc)
            .ThenByDescending(x => x.e.Id)
            .Skip(skip)
            .Take(normalizedPageSize)
            .Select(x => new CoinLedgerListItemResponse
            {
                Id = x.e.Id,
                UserId = x.UserId,
                UserEmail = x.Email,
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

    public async Task<PagedListResponse<CoinWalletAdminListItemResponse>> GetWalletsPagedAsync(
        PageRequest page,
        CoinWalletListQuery query,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = page.GetNormalizedPage();
        var normalizedPageSize = page.GetNormalizedPageSize();
        var skip = (normalizedPage - 1) * normalizedPageSize;

        var baseQ =
            from w in _db.UserCoinWallets.AsNoTracking()
            join u in _db.Users.AsNoTracking() on w.UserId equals u.Id
            select new { w, u.Email };

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = query.Q.Trim();
            baseQ = baseQ.Where(x => x.Email != null && x.Email.Contains(term));
        }

        var total = await baseQ.CountAsync(cancellationToken);
        var rows = await baseQ
            .OrderByDescending(x => x.w.UpdatedUtc)
            .Skip(skip)
            .Take(normalizedPageSize)
            .Select(x => new CoinWalletAdminListItemResponse
            {
                WalletId = x.w.Id,
                UserId = x.w.UserId,
                UserEmail = x.Email,
                BalanceCoins = x.w.BalanceCoins,
                UpdatedUtc = x.w.UpdatedUtc,
            })
            .ToListAsync(cancellationToken);

        return new PagedListResponse<CoinWalletAdminListItemResponse>
        {
            Items = rows,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = total,
        };
    }

    public async Task<PagedListResponse<ExternalPaymentAdminListItemResponse>> GetExternalPaymentsPagedAsync(
        PageRequest page,
        ExternalPaymentListQuery query,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = page.GetNormalizedPage();
        var normalizedPageSize = page.GetNormalizedPageSize();
        var skip = (normalizedPage - 1) * normalizedPageSize;

        var baseQ =
            from p in _db.ExternalPaymentRecords.AsNoTracking()
            join u in _db.Users.AsNoTracking() on p.UserId equals u.Id
            join st in _db.PaymentProcessingStatuses.AsNoTracking() on p.PaymentProcessingStatusId equals st.Id
            select new { p, u.Email, st.Code };

        if (!string.IsNullOrWhiteSpace(query.UserId))
            baseQ = baseQ.Where(x => x.p.UserId == query.UserId);

        if (query.PaymentProcessingStatusId.HasValue)
            baseQ = baseQ.Where(x => x.p.PaymentProcessingStatusId == query.PaymentProcessingStatusId.Value);

        if (!string.IsNullOrWhiteSpace(query.PurposeCode))
        {
            var pc = query.PurposeCode.Trim();
            baseQ = baseQ.Where(x => x.p.PurposeCode == pc);
        }

        if (!string.IsNullOrWhiteSpace(query.Provider))
        {
            var pr = query.Provider.Trim();
            baseQ = baseQ.Where(x => x.p.Provider == pr);
        }

        if (query.DateFromUtc.HasValue)
            baseQ = baseQ.Where(x => x.p.CreatedUtc >= query.DateFromUtc.Value);

        if (query.DateToUtc.HasValue)
            baseQ = baseQ.Where(x => x.p.CreatedUtc <= query.DateToUtc.Value);

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = query.Q.Trim();
            baseQ = baseQ.Where(x =>
                (x.Email != null && x.Email.Contains(term))
                || (x.p.ExternalReference != null && x.p.ExternalReference.Contains(term)));
        }

        if (query.ExcludeDemoSeed)
        {
            baseQ = baseQ.Where(x =>
                x.p.ExternalReference == null || !x.p.ExternalReference.StartsWith("SEED-"));
        }

        var total = await baseQ.CountAsync(cancellationToken);
        var rows = await baseQ
            .OrderBy(x => x.p.ExternalReference != null && x.p.ExternalReference.StartsWith("SEED-"))
            .ThenByDescending(x => x.p.CreatedUtc)
            .ThenByDescending(x => x.p.Id)
            .Skip(skip)
            .Take(normalizedPageSize)
            .Select(x => new ExternalPaymentAdminListItemResponse
            {
                Id = x.p.Id,
                UserId = x.p.UserId,
                UserEmail = x.Email,
                PurposeCode = x.p.PurposeCode,
                Provider = x.p.Provider,
                AmountMoney = x.p.AmountMoney,
                Currency = x.p.Currency,
                PaymentProcessingStatusId = x.p.PaymentProcessingStatusId,
                PaymentStatusCode = x.Code,
                ExternalReference = x.p.ExternalReference,
                CoinsPurchased = x.p.CoinsPurchased,
                CreatedUtc = x.p.CreatedUtc,
            })
            .ToListAsync(cancellationToken);

        return new PagedListResponse<ExternalPaymentAdminListItemResponse>
        {
            Items = rows,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = total,
        };
    }

    public async Task<IReadOnlyList<HallEarningsListItemResponse>> GetHallEarningsAsync(
        DateTime? dateFromUtc,
        DateTime? dateToUtc,
        CancellationToken cancellationToken = default)
    {
        var q = from e in _db.CoinLedgerEntries.AsNoTracking()
            where e.RelatedScheduledSessionId != null
                  && (
                      (e.ReasonCode == Domain.CoinLedgerReasonCodes.SessionJoin && e.AmountCoins < 0)
                      || (e.ReasonCode == Domain.CoinLedgerReasonCodes.SessionRefundCancel && e.AmountCoins > 0))
            join s in _db.ScheduledSessions.AsNoTracking() on e.RelatedScheduledSessionId equals s.Id
            select new { e, s.HallId, HallName = s.Hall.Name, CityName = s.Hall.City.Name, s.StartUtc };

        if (dateFromUtc.HasValue)
            q = q.Where(x => x.StartUtc >= dateFromUtc.Value);
        if (dateToUtc.HasValue)
            q = q.Where(x => x.StartUtc <= dateToUtc.Value);

        var grouped = await q
            .GroupBy(x => new { x.HallId, x.HallName, x.CityName })
            .Select(g => new HallEarningsListItemResponse
            {
                HallId = g.Key.HallId,
                HallName = g.Key.HallName,
                CityName = g.Key.CityName,
                SessionCount = g
                    .Where(x => x.e.ReasonCode == Domain.CoinLedgerReasonCodes.SessionJoin)
                    .Select(x => x.e.RelatedScheduledSessionId)
                    .Distinct()
                    .Count(),
                TotalCoinsEarned = g.Sum(x => -x.e.AmountCoins),
            })
            .OrderByDescending(x => x.TotalCoinsEarned)
            .ToListAsync(cancellationToken);

        return grouped;
    }
}

