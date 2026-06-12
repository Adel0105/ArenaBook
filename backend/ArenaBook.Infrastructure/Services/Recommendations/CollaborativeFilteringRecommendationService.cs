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

    private const double ParticipationPreferenceWeight = 4.0;
    private const double LikePreferenceWeight = 5.0;
    private const double DislikePreferenceWeight = 1.0;

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

        if (halls.Count == 0)
            return [];

        var interactions = await LoadCityInteractionsAsync(effectiveCityId.Value, cancellationToken);
        var matrix = CollaborativeFilteringEngine.BuildUserHallMatrix(interactions);
        var neighbors = CollaborativeFilteringEngine.FindSimilarUsers(userId, matrix);
        var prediction = CollaborativeFilteringEngine.PredictHallScores(
            userId,
            matrix,
            neighbors,
            halls.Select(h => h.Id));

        var maxEngagement = halls.Max(ComputeEngagementScore);
        if (maxEngagement <= 0)
            maxEngagement = 1;

        var ranked = halls
            .Select(h =>
            {
                var engagement = ComputeEngagementScore(h);
                var popularity = CollaborativeFilteringEngine.NormalizePopularityScore(engagement, maxEngagement);
                var collaborative = prediction.Scores.GetValueOrDefault(h.Id, 0);
                var finalScore = CollaborativeFilteringEngine.BlendScores(
                    collaborative,
                    popularity,
                    prediction.SimilarUserCount,
                    prediction.HasUserProfile);

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
                    Score = finalScore,
                    Explanation = BuildHallExplanation(
                        cityName,
                        h,
                        finalScore,
                        collaborative,
                        popularity,
                        prediction.SimilarUserCount,
                        prediction.HasUserProfile,
                        averageRating),
                };
            })
            .OrderByDescending(h => h.Score)
            .ThenByDescending(h => h.ReviewCount)
            .ThenBy(h => h.Name)
            .Take(take)
            .ToList();

        return ranked;
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

        var hallScores = await GetPersonalizedHallScoreMapAsync(userId, effectiveCityId.Value, cancellationToken);

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
                        ? $"Termin u dvorani iz {s.CityName} — personalizirani CF skor dvorane {hallScore:F1} (rezervacije, ocjene i reakcije sličnih igrača)."
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
        if (cityId.HasValue)
        {
            var cityExists = await _db.Cities.AsNoTracking()
                .AnyAsync(c => c.Id == cityId.Value, cancellationToken);
            if (cityExists)
                return cityId.Value;
        }

        return await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.CityId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Dictionary<int, double>> GetPersonalizedHallScoreMapAsync(
        string userId,
        int cityId,
        CancellationToken cancellationToken)
    {
        var halls = await _db.Halls.AsNoTracking()
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

        if (halls.Count == 0)
            return [];

        var interactions = await LoadCityInteractionsAsync(cityId, cancellationToken);
        var matrix = CollaborativeFilteringEngine.BuildUserHallMatrix(interactions);
        var neighbors = CollaborativeFilteringEngine.FindSimilarUsers(userId, matrix);
        var prediction = CollaborativeFilteringEngine.PredictHallScores(
            userId,
            matrix,
            neighbors,
            halls.Select(h => h.Id));

        var maxEngagement = halls.Max(ComputeEngagementScore);
        if (maxEngagement <= 0)
            maxEngagement = 1;

        return halls.ToDictionary(
            h => h.Id,
            h =>
            {
                var popularity = CollaborativeFilteringEngine.NormalizePopularityScore(
                    ComputeEngagementScore(h),
                    maxEngagement);
                var collaborative = prediction.Scores.GetValueOrDefault(h.Id, 0);
                return CollaborativeFilteringEngine.BlendScores(
                    collaborative,
                    popularity,
                    prediction.SimilarUserCount,
                    prediction.HasUserProfile);
            });
    }

    private async Task<IReadOnlyList<CollaborativeFilteringEngine.Interaction>> LoadCityInteractionsAsync(
        int cityId,
        CancellationToken cancellationToken)
    {
        var reviews = await _db.HallReviews.AsNoTracking()
            .Where(r => r.Hall.CityId == cityId && r.Hall.IsActive)
            .Select(r => new CollaborativeFilteringEngine.Interaction(
                r.UserId,
                r.HallId,
                r.RatingStars))
            .ToListAsync(cancellationToken);

        var reactions = await _db.HallReactions.AsNoTracking()
            .Where(r => r.Hall.CityId == cityId && r.Hall.IsActive)
            .Select(r => new CollaborativeFilteringEngine.Interaction(
                r.UserId,
                r.HallId,
                r.IsLike ? LikePreferenceWeight : DislikePreferenceWeight))
            .ToListAsync(cancellationToken);

        var participations = await _db.ScheduledSessionParticipants.AsNoTracking()
            .Where(p => p.ScheduledSession.Hall.CityId == cityId && p.ScheduledSession.Hall.IsActive)
            .Select(p => new CollaborativeFilteringEngine.Interaction(
                p.UserId,
                p.ScheduledSession.HallId,
                ParticipationPreferenceWeight))
            .ToListAsync(cancellationToken);

        var merged = new List<CollaborativeFilteringEngine.Interaction>(
            reviews.Count + reactions.Count + participations.Count);
        merged.AddRange(reviews);
        merged.AddRange(reactions);
        merged.AddRange(participations);
        return merged;
    }

    private static double ComputeEngagementScore(HallEngagementRow row) =>
        row.LikeCount * LikePoints
        + row.DislikeCount * DislikePoints
        + row.StarPoints * StarPoints
        + row.CommentCount * CommentPoints;

    private static string BuildHallExplanation(
        string cityName,
        HallEngagementRow row,
        double finalScore,
        double collaborativeScore,
        double popularityScore,
        int similarUserCount,
        bool hasUserProfile,
        double averageRating)
    {
        if (hasUserProfile && similarUserCount > 0 && collaborativeScore > 0)
        {
            return
                $"Dvorana u gradu {cityName}. Personalizirani CF skor {finalScore:F1} " +
                $"(predviđanje {collaborativeScore:F1} na osnovu {similarUserCount} sličnih igrača " +
                $"po rezervacijama, recenzijama i reakcijama; popularnost u gradu {popularityScore:F1}).";
        }

        if (hasUserProfile && similarUserCount > 0)
        {
            return
                $"Dvorana u gradu {cityName}. Skor {finalScore:F1} — preporuka iz susjedstva od {similarUserCount} " +
                $"sličnih igrača (kosinusna sličnost); popularnost u gradu {popularityScore:F1}.";
        }

        if (hasUserProfile)
        {
            return
                $"Dvorana u gradu {cityName}. Skor {finalScore:F1} — još nema dovoljno sličnih igrača za CF; " +
                $"rangirano prema popularnosti (★ {averageRating:F1}, {row.ReviewCount} recenzija).";
        }

        return
            $"Dvorana u gradu {cityName}. Popularnost {finalScore:F1} " +
            $"(👍 {row.LikeCount}, 👎 {row.DislikeCount}, ★ {averageRating:F1}, {row.ReviewCount} recenzija). " +
            "Personalizacija raste nakon vaših rezervacija, ocjena i reakcija.";
    }

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
