using ArenaBook.Application.Contracts.Sessions;
using FluentValidation;

namespace ArenaBook.Application.Validation;

public sealed class UpdateScheduledSessionRequestValidator : AbstractValidator<UpdateScheduledSessionRequest>
{
    public UpdateScheduledSessionRequestValidator()
    {
        RuleFor(x => x.EndUtc).GreaterThan(x => x.StartUtc);
        RuleFor(x => x.MaxParticipants).GreaterThan(0);
        RuleFor(x => x.MaxAgeYears).GreaterThan(0).When(x => x.MaxAgeYears.HasValue);
    }
}

