using ArenaBook.Application.Contracts.Reference;
using FluentValidation;

namespace ArenaBook.Application.Validation;

public sealed class CreateCountryRequestValidator : AbstractValidator<CreateCountryRequest>
{
    public CreateCountryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
    }
}


