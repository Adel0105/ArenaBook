namespace ArenaBook.Domain.Entities;

public sealed class Country
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<City> Cities { get; set; } = new List<City>();
}

