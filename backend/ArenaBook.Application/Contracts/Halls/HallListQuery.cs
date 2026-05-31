namespace ArenaBook.Application.Contracts.Halls;

public sealed class HallListQuery
{
    public string? Q { get; init; }

    public int? CountryId { get; init; }

    public int? CityId { get; init; }

    public bool? IsActive { get; init; }

    public int? MinCapacityPeople { get; init; }

    public int? MaxCapacityPeople { get; init; }

    public decimal? MinPricePerHourCoins { get; init; }

    public decimal? MaxPricePerHourCoins { get; init; }
}


