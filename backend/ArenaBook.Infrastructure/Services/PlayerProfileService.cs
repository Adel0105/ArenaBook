using ArenaBook.Application.Abstractions;
using ArenaBook.Application.Contracts.Auth;
using ArenaBook.Domain;
using ArenaBook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArenaBook.Infrastructure.Services;

public sealed class PlayerProfileService : IPlayerProfileService
{
    private readonly ArenaBookDbContext _db;

    public PlayerProfileService(ArenaBookDbContext db)
    {
        _db = db;
    }

    public async Task<PlayerProfileStatsResponse> GetStatsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var completedId = await _db.SessionLifecycleStatuses.AsNoTracking()
            .Where(s => s.Code == "COMPLETED")
            .Select(s => s.Id)
            .FirstAsync(cancellationToken);

        var confirmedId = await _db.SessionLifecycleStatuses.AsNoTracking()
            .Where(s => s.Code == "CONFIRMED")
            .Select(s => s.Id)
            .FirstAsync(cancellationToken);

        var participantSessionIds = await _db.ScheduledSessionParticipants.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => new { p.ScheduledSessionId, p.ScheduledSession.SessionLifecycleStatusId, p.ScheduledSession.StartUtc })
            .ToListAsync(cancellationToken);

        var totalParticipations = participantSessionIds.Count;
        var completedParticipations = participantSessionIds.Count(p => p.SessionLifecycleStatusId == completedId);
        var upcomingParticipations = participantSessionIds.Count(
            p => p.SessionLifecycleStatusId == confirmedId && p.StartUtc >= DateTime.UtcNow);

        var organizedSessions = await _db.ScheduledSessions.AsNoTracking()
            .CountAsync(s => s.OrganizerUserId == userId, cancellationToken);

        var spent = await _db.CoinLedgerEntries.AsNoTracking()
            .Where(e => e.UserCoinWallet.UserId == userId && e.ReasonCode == CoinLedgerReasonCodes.SessionJoin)
            .SumAsync(e => (decimal?)Math.Abs(e.AmountCoins), cancellationToken) ?? 0;

        var purchased = await _db.CoinLedgerEntries.AsNoTracking()
            .Where(e => e.UserCoinWallet.UserId == userId && e.ReasonCode == CoinLedgerReasonCodes.CoinPurchaseCredit)
            .SumAsync(e => (decimal?)e.AmountCoins, cancellationToken) ?? 0;

        var firstJoin = participantSessionIds.MinBy(p => p.StartUtc)?.StartUtc;
        double frequency = 0;
        if (firstJoin.HasValue && totalParticipations > 0)
        {
            var months = Math.Max(1, (DateTime.UtcNow - firstJoin.Value).TotalDays / 30.0);
            frequency = Math.Round(totalParticipations / months, 2);
        }

        return new PlayerProfileStatsResponse
        {
            TotalParticipations = totalParticipations,
            CompletedParticipations = completedParticipations,
            OrganizedSessions = organizedSessions,
            UpcomingParticipations = upcomingParticipations,
            TotalCoinsSpentOnSessions = spent,
            TotalCoinsPurchased = purchased,
            PlayFrequencyPerMonth = frequency,
        };
    }
}

