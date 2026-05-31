namespace ArenaBook.Application.Contracts.Halls;

public sealed class UpdateHallRequest
{
    public string Name { get; set; } = string.Empty;

    public int CityId { get; set; }

    public string StreetAddress { get; set; } = string.Empty;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public int CapacityPeople { get; set; }

    public decimal PricePerHourCoins { get; set; }

    public string ContactPhone { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}


