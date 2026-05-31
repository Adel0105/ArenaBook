using ArenaBook.Application.Contracts.Reference;
using FluentValidation;

namespace ArenaBook.Application.Validation;

public sealed class UpdateEquipmentTypeRequestValidator : AbstractValidator<UpdateEquipmentTypeRequest>
{
    public UpdateEquipmentTypeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
    }
}


