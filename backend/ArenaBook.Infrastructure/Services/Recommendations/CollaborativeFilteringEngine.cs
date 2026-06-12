namespace ArenaBook.Infrastructure.Services.Recommendations;

internal static class CollaborativeFilteringEngine
{
    public const int DefaultMaxNeighbors = 15;
    public const double DefaultMinSimilarity = 0.05;

    public sealed record Interaction(string UserId, int HallId, double Weight);

    public sealed record PredictionResult(
        IReadOnlyDictionary<int, double> Scores,
        int SimilarUserCount,
        bool HasUserProfile);

    public static Dictionary<string, Dictionary<int, double>> BuildUserHallMatrix(
        IEnumerable<Interaction> interactions)
    {
        var matrix = new Dictionary<string, Dictionary<int, double>>(StringComparer.Ordinal);

        foreach (var interaction in interactions)
        {
            if (!matrix.TryGetValue(interaction.UserId, out var halls))
            {
                halls = new Dictionary<int, double>();
                matrix[interaction.UserId] = halls;
            }

            if (!halls.TryGetValue(interaction.HallId, out var existing))
                halls[interaction.HallId] = interaction.Weight;
            else if (interaction.Weight > existing)
                halls[interaction.HallId] = interaction.Weight;
        }

        return matrix;
    }

    public static List<(string UserId, double Similarity)> FindSimilarUsers(
        string targetUserId,
        IReadOnlyDictionary<string, Dictionary<int, double>> matrix,
        int maxNeighbors = DefaultMaxNeighbors,
        double minSimilarity = DefaultMinSimilarity)
    {
        if (!matrix.TryGetValue(targetUserId, out var targetPrefs) || targetPrefs.Count == 0)
            return [];

        var similarities = new List<(string UserId, double Similarity)>();

        foreach (var (otherUserId, otherPrefs) in matrix)
        {
            if (otherUserId == targetUserId || otherPrefs.Count == 0)
                continue;

            var similarity = CosineSimilarity(targetPrefs, otherPrefs);
            if (similarity >= minSimilarity)
                similarities.Add((otherUserId, similarity));
        }

        return similarities
            .OrderByDescending(x => x.Similarity)
            .Take(maxNeighbors)
            .ToList();
    }

    public static PredictionResult PredictHallScores(
        string targetUserId,
        IReadOnlyDictionary<string, Dictionary<int, double>> matrix,
        IReadOnlyList<(string UserId, double Similarity)> neighbors,
        IEnumerable<int> candidateHallIds)
    {
        matrix.TryGetValue(targetUserId, out var targetPrefs);
        targetPrefs ??= new Dictionary<int, double>();

        var scores = new Dictionary<int, double>();
        foreach (var hallId in candidateHallIds)
        {
            if (targetPrefs.ContainsKey(hallId))
                continue;

            double weightedSum = 0;
            double similaritySum = 0;

            foreach (var (neighborId, similarity) in neighbors)
            {
                if (!matrix.TryGetValue(neighborId, out var neighborPrefs))
                    continue;

                if (!neighborPrefs.TryGetValue(hallId, out var rating))
                    continue;

                weightedSum += similarity * rating;
                similaritySum += similarity;
            }

            if (similaritySum > 0)
                scores[hallId] = weightedSum / similaritySum;
        }

        return new PredictionResult(
            scores,
            neighbors.Count,
            targetPrefs.Count > 0);
    }

    public static double BlendScores(
        double collaborativeScore,
        double popularityScore,
        int similarUserCount,
        bool hasUserProfile)
    {
        if (!hasUserProfile || similarUserCount == 0)
            return popularityScore;

        var cfWeight = Math.Clamp(similarUserCount / 5.0, 0.25, 0.85);
        if (collaborativeScore <= 0)
            cfWeight *= 0.35;

        return cfWeight * collaborativeScore + (1 - cfWeight) * popularityScore;
    }

    public static double NormalizePopularityScore(double engagementScore, double maxEngagementInCity)
    {
        if (engagementScore <= 0 || maxEngagementInCity <= 0)
            return 0;

        return Math.Clamp(engagementScore / maxEngagementInCity * 5.0, 0, 5);
    }

    private static double CosineSimilarity(
        IReadOnlyDictionary<int, double> left,
        IReadOnlyDictionary<int, double> right)
    {
        double dot = 0;
        double magnitudeLeft = 0;
        double magnitudeRight = 0;

        foreach (var (hallId, ratingLeft) in left)
        {
            magnitudeLeft += ratingLeft * ratingLeft;
            if (right.TryGetValue(hallId, out var ratingRight))
                dot += ratingLeft * ratingRight;
        }

        foreach (var ratingRight in right.Values)
            magnitudeRight += ratingRight * ratingRight;

        if (magnitudeLeft == 0 || magnitudeRight == 0)
            return 0;

        return dot / (Math.Sqrt(magnitudeLeft) * Math.Sqrt(magnitudeRight));
    }
}
