namespace ArenaBook.Application.Contracts.Reference;

public sealed class CreateCityRequest
{
    public int CountryId { get; set; }

    public string Name { get; set; } = string.Empty;
}


