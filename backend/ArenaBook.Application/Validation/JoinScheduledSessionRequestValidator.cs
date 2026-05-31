using ArenaBook.Application.Contracts.Sessions;
using FluentValidation;

namespace ArenaBook.Application.Validation;

public sealed class JoinScheduledSessionRequestValidator : AbstractValidator<JoinScheduledSessionRequest>
{
    public JoinScheduledSessionRequestValidator()
    {
        RuleFor(x => x.InviteCode).MaximumLength(32).When(x => !string.IsNullOrEmpty(x.InviteCode));
    }
}

