namespace ArenaBook.Application.Contracts.Halls;

public sealed class HallPhotoResponse
{
    public int Id { get; set; }

    public int HallId { get; set; }

    public int SortOrder { get; set; }

    public string ImageUrl { get; set; } = string.Empty;
}


