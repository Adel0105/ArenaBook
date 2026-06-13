using ArenaBook.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace ArenaBook.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    public int? CityId { get; set; }

    public City? City { get; set; }

    public string? ProfileImageUrl { get; set; }

    public DateTime CreatedUtc { get; set; }
}

