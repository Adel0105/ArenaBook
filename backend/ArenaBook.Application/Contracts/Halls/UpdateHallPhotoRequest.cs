namespace ArenaBook.Application.Contracts.Halls;

public sealed class UpdateHallPhotoRequest
{
    public int SortOrder { get; set; }

    public string ImageUrl { get; set; } = string.Empty;
}


