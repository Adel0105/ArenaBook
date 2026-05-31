namespace ArenaBook.Application.Contracts.Halls;

public sealed class HallListItemResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int CityId { get; set; }

    public string CityName { get; set; } = string.Empty;

    public int CountryId { get; set; }

    public string CountryName { get; set; } = string.Empty;

    public string StreetAddress { get; set; } = string.Empty;

    public int CapacityPeople { get; set; }

    public decimal PricePerHourCoins { get; set; }

    public string ContactPhone { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public string? PrimaryImageUrl { get; set; }
}


