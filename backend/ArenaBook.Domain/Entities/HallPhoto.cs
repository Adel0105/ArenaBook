namespace ArenaBook.Domain.Entities;

public sealed class HallPhoto
{
    public int Id { get; set; }

    public int HallId { get; set; }

    public Hall Hall { get; set; } = null!;

    public int SortOrder { get; set; }

    public string ImageUrl { get; set; } = string.Empty;
}

