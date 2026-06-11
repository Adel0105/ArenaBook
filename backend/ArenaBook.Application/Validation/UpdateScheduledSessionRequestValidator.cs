using ArenaBook.Application.Contracts.Sessions;
using ArenaBook.Application.Sessions;
using FluentValidation;

namespace ArenaBook.Application.Validation;

public sealed class UpdateScheduledSessionRequestValidator : AbstractValidator<UpdateScheduledSessionRequest>
{
    public UpdateScheduledSessionRequestValidator()
    {
        RuleFor(x => x.MaxParticipants).GreaterThan(0);
        RuleFor(x => x.MaxAgeYears).GreaterThan(0).When(x => x.MaxAgeYears.HasValue);

        RuleFor(x => x)
            .Custom((request, context) =>
            {
                foreach (var (key, messages) in SessionTimeRules.ValidateStructure(request.StartUtc, request.EndUtc))
                    context.AddFailure(key, messages[0]);
            });
    }
}
