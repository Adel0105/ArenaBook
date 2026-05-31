namespace ArenaBook.Application.Contracts.Admin;

public sealed class CreateAdminUserRequest
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    public int? CityId { get; set; }

    public string RoleName { get; set; } = string.Empty;
}

