using ArenaBook.Application.Contracts.Reference;
using FluentValidation;

namespace ArenaBook.Application.Validation;

public sealed class CreateEquipmentTypeRequestValidator : AbstractValidator<CreateEquipmentTypeRequest>
{
    public CreateEquipmentTypeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
    }
}


