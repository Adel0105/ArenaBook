using ArenaBook.Application.Contracts.Auth;
using FluentValidation;

namespace ArenaBook.Application.Validation;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(256).EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}

