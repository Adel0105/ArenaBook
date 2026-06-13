using ArenaBook.Application.Contracts.Sessions;
using FluentValidation;

namespace ArenaBook.Application.Validation;

public sealed class CancelScheduledSessionRequestValidator : AbstractValidator<CancelScheduledSessionRequest>
{
    public CancelScheduledSessionRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Razlog otkazivanja je obavezan.")
            .MaximumLength(500);
    }
}
