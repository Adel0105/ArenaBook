using ArenaBook.Application.Abstractions.Admin;
using ArenaBook.Application.Contracts.Admin;
using ArenaBook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArenaBook.Infrastructure.Services.Admin;

public sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly ArenaBookDbContext _db;

    public AdminDashboardService(ArenaBookDbContext db)
    {
        _db = db;
    }

    public async Task<AdminDashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var cancelled = await _db.SessionLifecycleStatuses.AsNoTracking()
            .Where(x => x.Code == "CANCELLED")
            .Select(x => x.Id)
            .FirstAsync(cancellationToken);
        var completed = await _db.SessionLifecycleStatuses.AsNoTracking()
            .Where(x => x.Code == "COMPLETED")
            .Select(x => x.Id)
            .FirstAsync(cancellationToken);

        var totalUsers = await _db.Users.CountAsync(cancellationToken);
        var activeSessions = await _db.ScheduledSessions.CountAsync(
            s => s.SessionLifecycleStatusId != cancelled && s.SessionLifecycleStatusId != completed,
            cancellationToken);
        var halls = await _db.Halls.CountAsync(cancellationToken);
        var payments = await _db.ExternalPaymentRecords.CountAsync(cancellationToken);

        return new AdminDashboardSummaryResponse
        {
            TotalUsers = totalUsers,
            ActiveSessionsCount = activeSessions,
            TotalHalls = halls,
            ExternalPaymentsCount = payments,
        };
    }

    public async Task<AdminDashboardActivityResponse> GetActivityAsync(int months, CancellationToken cancellationToken = default)
    {
        var count = months < 1 ? 6 : (months > 24 ? 24 : months);
        var end = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var start = end.AddMonths(-(count - 1));

        var usersRaw = await _db.UserCoinWallets.AsNoTracking()
            .Where(w => w.UpdatedUtc >= start)
            .GroupBy(w => new { w.UpdatedUtc.Year, w.UpdatedUtc.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, C = g.Count() })
            .ToListAsync(cancellationToken);

        var sessionsRaw = await _db.ScheduledSessions.AsNoTracking()
            .Where(s => s.CreatedUtc >= start)
            .GroupBy(s => new { s.CreatedUtc.Year, s.CreatedUtc.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, C = g.Count() })
            .ToListAsync(cancellationToken);

        var paymentsRaw = await _db.ExternalPaymentRecords.AsNoTracking()
            .Where(p => p.CreatedUtc >= start)
            .GroupBy(p => new { p.CreatedUtc.Year, p.CreatedUtc.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, C = g.Count() })
            .ToListAsync(cancellationToken);

        return new AdminDashboardActivityResponse
        {
            UsersByMonth = BuildSeries(start, count, usersRaw.Select(x => (x.Year, x.Month, x.C))),
            SessionsByMonth = BuildSeries(start, count, sessionsRaw.Select(x => (x.Year, x.Month, x.C))),
            PaymentsByMonth = BuildSeries(start, count, paymentsRaw.Select(x => (x.Year, x.Month, x.C))),
        };
    }

    private static IReadOnlyList<AdminDashboardMonthlyPoint> BuildSeries(
        DateTime start,
        int months,
        IEnumerable<(int Year, int Month, int Count)> data)
    {
        var map = data.ToDictionary(x => (x.Year, x.Month), x => x.Count);
        var list = new List<AdminDashboardMonthlyPoint>();
        for (var i = 0; i < months; i++)
        {
            var d = start.AddMonths(i);
            var key = (d.Year, d.Month);
            map.TryGetValue(key, out var c);
            list.Add(new AdminDashboardMonthlyPoint
            {
                Year = d.Year,
                Month = d.Month,
                Label = $"{d.Month:00}/{d.Year}",
                Count = c,
            });
        }

        return list;
    }
}

