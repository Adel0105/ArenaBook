namespace ArenaBook.Application.Contracts.Reference;

public sealed class CityResponse
{
    public int Id { get; set; }

    public int CountryId { get; set; }

    public string Name { get; set; } = string.Empty;
}


