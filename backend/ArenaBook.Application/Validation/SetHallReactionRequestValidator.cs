using ArenaBook.Application.Contracts.Halls;
using FluentValidation;

namespace ArenaBook.Application.Validation;

public sealed class SetHallReactionRequestValidator : AbstractValidator<SetHallReactionRequest>
{
    private static readonly string[] Allowed = ["like", "dislike", "none"];

    public SetHallReactionRequestValidator()
    {
        RuleFor(x => x.Reaction)
            .NotEmpty()
            .Must(r => Allowed.Contains(r.Trim().ToLowerInvariant()))
            .WithMessage("Reakcija mora biti like, dislike ili none.");
    }
}

