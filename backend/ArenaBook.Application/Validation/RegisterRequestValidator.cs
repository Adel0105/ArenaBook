using ArenaBook.Application.Contracts.Auth;
using FluentValidation;

namespace ArenaBook.Application.Validation;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(256).EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.DateOfBirth)
            .NotNull()
            .WithMessage("Datum rođenja je obavezan.")
            .Must(d => d!.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Datum rođenja ne može biti u budućnosti.")
            .Must(d => d!.Value <= DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-13)))
            .WithMessage("Morate imati najmanje 13 godina.");
        RuleFor(x => x.CityId).GreaterThan(0).When(x => x.CityId.HasValue);
    }
}

