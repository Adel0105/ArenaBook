using ArenaBook.Application.Contracts.Sessions;
using ArenaBook.Application.Sessions;
using FluentValidation;

namespace ArenaBook.Application.Validation;

public sealed class CreateScheduledSessionRequestValidator : AbstractValidator<CreateScheduledSessionRequest>
{
    public CreateScheduledSessionRequestValidator()
    {
        RuleFor(x => x.HallId).GreaterThan(0);
        RuleFor(x => x.SessionKindId).GreaterThan(0);
        RuleFor(x => x.MaxParticipants).GreaterThan(0);
        RuleFor(x => x.MaxAgeYears).GreaterThan(0).When(x => x.MaxAgeYears.HasValue);
        RuleFor(x => x.InviteCode).MaximumLength(32).When(x => !string.IsNullOrEmpty(x.InviteCode));
        RuleFor(x => x.OrganizerUserId).MaximumLength(450).When(x => !string.IsNullOrWhiteSpace(x.OrganizerUserId));

        RuleFor(x => x)
            .Custom((request, context) =>
            {
                foreach (var (key, messages) in SessionTimeRules.ValidateStructure(request.StartUtc, request.EndUtc))
                    context.AddFailure(key, messages[0]);
            });
    }
}
