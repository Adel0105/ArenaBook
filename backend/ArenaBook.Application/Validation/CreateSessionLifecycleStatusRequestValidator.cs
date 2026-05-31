using ArenaBook.Application.Contracts.Reference;
using FluentValidation;

namespace ArenaBook.Application.Validation;

public sealed class CreateSessionLifecycleStatusRequestValidator : AbstractValidator<CreateSessionLifecycleStatusRequest>
{
    public CreateSessionLifecycleStatusRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(120);
    }
}


