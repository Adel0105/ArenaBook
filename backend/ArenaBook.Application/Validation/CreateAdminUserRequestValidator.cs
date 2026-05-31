using ArenaBook.Application.Contracts.Admin;
using ArenaBook.Domain.Security;
using FluentValidation;

namespace ArenaBook.Application.Validation;

public sealed class CreateAdminUserRequestValidator : AbstractValidator<CreateAdminUserRequest>
{
    public CreateAdminUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.DateOfBirth)
            .NotNull()
            .WithMessage("Datum rođenja je obavezan.")
            .Must(d => d!.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Datum rođenja ne može biti u budućnosti.")
            .Must(d => d!.Value <= DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-13)))
            .WithMessage("Korisnik mora imati najmanje 13 godina.");
        RuleFor(x => x.RoleName)
            .NotEmpty()
            .Must(r => r == ApplicationRoles.Member || r == ApplicationRoles.Organizer || r == ApplicationRoles.Administrator)
            .WithMessage("Uloga mora biti Member, Organizer ili Administrator.");
    }
}

