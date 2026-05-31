namespace ArenaBook.Application.Contracts.Coins;

public sealed class HallEarningsListItemResponse
{
    public int HallId { get; set; }

    public string HallName { get; set; } = string.Empty;

    public string CityName { get; set; } = string.Empty;

    public int SessionCount { get; set; }

    public decimal TotalCoinsEarned { get; set; }
}

