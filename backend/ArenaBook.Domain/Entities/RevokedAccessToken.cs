namespace ArenaBook.Domain.Entities;

public sealed class RevokedAccessToken
{
    public int Id { get; set; }

    public string JwtId { get; set; } = string.Empty;

    public DateTime ExpiresUtc { get; set; }

    public DateTime RevokedUtc { get; set; }
}
