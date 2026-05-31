using ArenaBook.Application.Contracts.Admin;
using FluentValidation;

namespace ArenaBook.Application.Validation;

public sealed class UpdateAdminUserRequestValidator : AbstractValidator<UpdateAdminUserRequest>
{
    public UpdateAdminUserRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.DateOfBirth)
            .NotNull()
            .WithMessage("Datum rođenja je obavezan.")
            .Must(d => d!.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Datum rođenja ne može biti u budućnosti.")
            .Must(d => d!.Value <= DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-13)))
            .WithMessage("Korisnik mora imati najmanje 13 godina.");
        RuleFor(x => x.ProfileImageUrl).MaximumLength(2048).When(x => !string.IsNullOrWhiteSpace(x.ProfileImageUrl));
    }
}

