namespace ArenaBook.Application.Contracts.Admin;

public sealed class AdminUserListItemResponse
{
    public string UserId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    public int? CityId { get; set; }

    public string? CityName { get; set; }

    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();

    public bool IsLockedOut { get; set; }

    public DateTimeOffset? LockoutEnd { get; set; }

    public DateTime? RegisteredUtc { get; set; }
}

