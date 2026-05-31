namespace ArenaBook.Application.Contracts.Auth;

public sealed class CurrentUserResponse
{
    public string UserId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    public int? CityId { get; set; }

    public string? ProfileImageUrl { get; set; }

    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}

