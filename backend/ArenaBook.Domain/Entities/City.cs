namespace ArenaBook.Domain.Entities;

public sealed class City
{
    public int Id { get; set; }

    public int CountryId { get; set; }

    public Country Country { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public ICollection<Hall> Halls { get; set; } = new List<Hall>();
}

