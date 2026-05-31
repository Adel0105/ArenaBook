namespace ArenaBook.Domain.Entities;

public sealed class HallReaction
{
    public int Id { get; set; }

    public int HallId { get; set; }

    public Hall Hall { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;

    public bool IsLike { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }
}

