using ArenaBook.Application.Abstractions.Recommendations;
using ArenaBook.Application.Contracts.Recommendations;
using ArenaBook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArenaBook.Infrastructure.Services.Recommendations;

public sealed class CollaborativeFilteringRecommendationService : IRecommendationService
{
    private const int LikePoints = 10;
    private const int DislikePoints = -10;
    private const int StarPoints = 1;
    private const int CommentPoints = 1;

    private readonly ArenaBookDbContext _db;

    public CollaborativeFilteringRecommendationService(ArenaBookDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RecommendedHallResponse>> GetRecommendedHallsAsync(
        string userId,
        int? cityId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(limit, 1, 50);
        var effectiveCityId = await ResolveUserCityIdAsync(userId, cityId, cancellationToken);
        if (!effectiveCityId.HasValue)
            return [];

        var cityName = await _db.Cities.AsNoTracking()
            .Where(c => c.Id == effectiveCityId.Value)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? "vaš grad";

        var halls = await _db.Halls.AsNoTracking()
            .Where(h => h.IsActive && h.CityId == effectiveCityId.Value)
            .Select(h => new HallEngagementRow
            {
                Id = h.Id,
                Name = h.Name,
                CityName = h.City.Name,
                CountryName = h.City.Country.Name,
                PricePerHourCoins = h.PricePerHourCoins,
                IsActive = h.IsActive,
                StarPoints = h.Reviews.Sum(r => (int)r.RatingStars),
                CommentCount = h.Reviews.Count(r => r.Comment != null && r.Comment != ""),
                LikeCount = h.Reactions.Count(r => r.IsLike),
                DislikeCount = h.Reactions.Count(r => !r.IsLike),
                ReviewCount = h.Reviews.Count,
            })
            .ToListAsync(cancellationToken);

        return halls
            .Select(h =>
            {
                var score = ComputeEngagementScore(h);
                var averageRating = h.ReviewCount > 0
                    ? (double)h.StarPoints / h.ReviewCount
                    : 0;
                return new RecommendedHallResponse
                {
                    HallId = h.Id,
                    Name = h.Name,
                    CityName = h.CityName,
                    CountryName = h.CountryName,
                    PricePerHourCoins = h.PricePerHourCoins,
                    AverageRating = averageRating,
                    ReviewCount = h.ReviewCount,
                    IsActive = h.IsActive,
                    Score = score,
                    Explanation = BuildHallExplanation(cityName, h, score),
                };
            })
            .OrderByDescending(h => h.Score)
            .ThenByDescending(h => h.ReviewCount)
            .ThenBy(h => h.Name)
            .Take(take)
            .ToList();
    }

    public async Task<IReadOnlyList<RecommendedSessionResponse>> GetRecommendedSessionsAsync(
        string userId,
        int? cityId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(limit, 1, 50);
        var effectiveCityId = await ResolveUserCityIdAsync(userId, cityId, cancellationToken);
        if (!effectiveCityId.HasValue)
            return [];

        var hallScores = await GetHallScoreMapAsync(effectiveCityId.Value, cancellationToken);

        var confirmedId = await _db.SessionLifecycleStatuses.AsNoTracking()
            .Where(s => s.Code == "CONFIRMED")
            .Select(s => s.Id)
            .FirstAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var joinedIds = await _db.ScheduledSessionParticipants.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => p.ScheduledSessionId)
            .ToListAsync(cancellationToken);

        var sessions = await _db.ScheduledSessions.AsNoTracking()
            .Where(s =>
                s.SessionLifecycleStatusId == confirmedId &&
                s.StartUtc >= now &&
                s.Hall.CityId == effectiveCityId.Value &&
                s.Hall.IsActive &&
                !joinedIds.Contains(s.Id))
            .Select(s => new
            {
                s.Id,
                s.HallId,
                HallName = s.Hall.Name,
                CityName = s.Hall.City.Name,
                KindCode = s.SessionKind.Code,
                s.StartUtc,
                s.EndUtc,
                ParticipantCount = s.Participants.Count,
                s.MaxParticipants,
                OrganizerEmail = _db.Users.AsNoTracking()
                    .Where(u => u.Id == s.OrganizerUserId)
                    .Select(u => u.Email)
                    .FirstOrDefault(),
                s.PriceTotalCoins,
                s.PricePerParticipantCoins,
            })
            .ToListAsync(cancellationToken);

        return sessions
            .Select(s =>
            {
                var hallScore = hallScores.GetValueOrDefault(s.HallId, 0);
                return new RecommendedSessionResponse
                {
                    SessionId = s.Id,
                    HallId = s.HallId,
                    HallName = s.HallName,
                    CityName = s.CityName,
                    SessionKindCode = s.KindCode,
                    StartUtc = s.StartUtc,
                    EndUtc = s.EndUtc,
                    ParticipantCount = s.ParticipantCount,
                    MaxParticipants = s.MaxParticipants,
                    PriceTotalCoins = s.PriceTotalCoins,
                    OrganizerEmail = s.OrganizerEmail,
                    Score = hallScore,
                    Explanation = hallScore > 0
                        ? $"Termin u dvorani iz {s.CityName} s visokim brojem bodova ({hallScore:F0}) na osnovu ocjena i lajkova."
                        : $"Termin u dvorani iz {s.CityName} — organizovan od strane drugih igrača.",
                };
            })
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.StartUtc)
            .Take(take)
            .ToList();
    }

    private async Task<int?> ResolveUserCityIdAsync(
        string userId,
        int? cityId,
        CancellationToken cancellationToken)
    {
        var profileCityId = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.CityId)
            .FirstOrDefaultAsync(cancellationToken);

        return profileCityId ?? cityId;
    }

    private async Task<Dictionary<int, double>> GetHallScoreMapAsync(
        int cityId,
        CancellationToken cancellationToken)
    {
        var rows = await _db.Halls.AsNoTracking()
            .Where(h => h.IsActive && h.CityId == cityId)
            .Select(h => new HallEngagementRow
            {
                Id = h.Id,
                StarPoints = h.Reviews.Sum(r => (int)r.RatingStars),
                CommentCount = h.Reviews.Count(r => r.Comment != null && r.Comment != ""),
                LikeCount = h.Reactions.Count(r => r.IsLike),
                DislikeCount = h.Reactions.Count(r => !r.IsLike),
            })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.Id, ComputeEngagementScore);
    }

    private static double ComputeEngagementScore(HallEngagementRow row) =>
        row.LikeCount * LikePoints
        + row.DislikeCount * DislikePoints
        + row.StarPoints * StarPoints
        + row.CommentCount * CommentPoints;

    private static string BuildHallExplanation(string cityName, HallEngagementRow row, double score) =>
        $"Dvorana u gradu {cityName}. Bodovi: {score:F0} " +
        $"(👍 {row.LikeCount}×{LikePoints}, 👎 {row.DislikeCount}×{DislikePoints}, " +
        $"★ {row.StarPoints}×{StarPoints}, 💬 {row.CommentCount}×{CommentPoints}).";

    private sealed class HallEngagementRow
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string CityName { get; init; } = string.Empty;
        public string CountryName { get; init; } = string.Empty;
        public decimal PricePerHourCoins { get; init; }
        public bool IsActive { get; init; }
        public int StarPoints { get; init; }
        public int CommentCount { get; init; }
        public int LikeCount { get; init; }
        public int DislikeCount { get; init; }
        public int ReviewCount { get; init; }
    }
}

