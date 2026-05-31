namespace ArenaBook.Application.Contracts.Admin;

public sealed class AdminUserListQuery
{
    public string? Q { get; set; }

    public string? Email { get; set; }

    public DateTime? RegisteredFromUtc { get; set; }

    public DateTime? RegisteredToUtc { get; set; }

    public bool? IsLockedOut { get; set; }
}

