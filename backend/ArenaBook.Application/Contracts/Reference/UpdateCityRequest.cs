namespace ArenaBook.Application.Contracts.Reference;

public sealed class UpdateCityRequest
{
    public int CountryId { get; set; }

    public string Name { get; set; } = string.Empty;
}


