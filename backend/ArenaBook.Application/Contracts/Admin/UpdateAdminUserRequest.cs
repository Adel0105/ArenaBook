namespace ArenaBook.Application.Contracts.Admin;

public sealed class UpdateAdminUserRequest
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    public int? CityId { get; set; }

    public string? ProfileImageUrl { get; set; }
}

