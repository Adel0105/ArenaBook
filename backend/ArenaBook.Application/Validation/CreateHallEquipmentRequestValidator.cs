using ArenaBook.Application.Contracts.Halls;
using FluentValidation;

namespace ArenaBook.Application.Validation;

public sealed class CreateHallEquipmentRequestValidator : AbstractValidator<CreateHallEquipmentRequest>
{
    public CreateHallEquipmentRequestValidator()
    {
        RuleFor(x => x.EquipmentTypeId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
    }
}


