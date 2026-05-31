using ArenaBook.Application.Contracts.Halls;
using FluentValidation;

namespace ArenaBook.Application.Validation;

public sealed class UpdateHallEquipmentRequestValidator : AbstractValidator<UpdateHallEquipmentRequest>
{
    public UpdateHallEquipmentRequestValidator()
    {
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
    }
}


