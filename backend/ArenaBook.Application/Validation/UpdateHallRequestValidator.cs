using ArenaBook.Application.Contracts.Halls;
using FluentValidation;

namespace ArenaBook.Application.Validation;

public sealed class UpdateHallRequestValidator : AbstractValidator<UpdateHallRequest>
{
    public UpdateHallRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CityId).GreaterThan(0);
        RuleFor(x => x.StreetAddress).NotEmpty().MaximumLength(300);
        RuleFor(x => x.ContactPhone).NotEmpty().MaximumLength(40);
        RuleFor(x => x.CapacityPeople).GreaterThan(0);
        RuleFor(x => x.PricePerHourCoins).GreaterThanOrEqualTo(0);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90m, 90m)
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180m, 180m)
            .When(x => x.Longitude.HasValue);
    }
}


