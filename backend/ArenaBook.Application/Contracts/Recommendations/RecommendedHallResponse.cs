namespace ArenaBook.Application.Contracts.Recommendations;

public sealed class RecommendedHallResponse
{
    public int HallId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string CityName { get; set; } = string.Empty;

    public string CountryName { get; set; } = string.Empty;

    public decimal PricePerHourCoins { get; set; }

    public double AverageRating { get; set; }

    public int ReviewCount { get; set; }

    public bool IsActive { get; set; }

    public double Score { get; set; }

    public string Explanation { get; set; } = string.Empty;
}

