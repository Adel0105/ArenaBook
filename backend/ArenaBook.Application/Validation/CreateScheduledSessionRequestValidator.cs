using ArenaBook.Application.Contracts.Sessions;
using FluentValidation;

namespace ArenaBook.Application.Validation;

public sealed class CreateScheduledSessionRequestValidator : AbstractValidator<CreateScheduledSessionRequest>
{
    public CreateScheduledSessionRequestValidator()
    {
        RuleFor(x => x.HallId).GreaterThan(0);
        RuleFor(x => x.SessionKindId).GreaterThan(0);
        RuleFor(x => x.EndUtc).GreaterThan(x => x.StartUtc);
        RuleFor(x => x.MaxParticipants).GreaterThan(0);
        RuleFor(x => x.MaxAgeYears).GreaterThan(0).When(x => x.MaxAgeYears.HasValue);
        RuleFor(x => x.InviteCode).MaximumLength(32).When(x => !string.IsNullOrEmpty(x.InviteCode));
    }
}

