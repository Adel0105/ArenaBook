using ArenaBook.Application.Contracts.Reference;
using FluentValidation;

namespace ArenaBook.Application.Validation;

public sealed class CreateCityRequestValidator : AbstractValidator<CreateCityRequest>
{
    public CreateCityRequestValidator()
    {
        RuleFor(x => x.CountryId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
    }
}


