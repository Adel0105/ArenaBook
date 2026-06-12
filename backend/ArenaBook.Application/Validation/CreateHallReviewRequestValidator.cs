using ArenaBook.Application.Contracts.Halls;
using FluentValidation;

namespace ArenaBook.Application.Validation;

public sealed class CreateHallReviewRequestValidator : AbstractValidator<CreateHallReviewRequest>
{
    public CreateHallReviewRequestValidator()
    {
        RuleFor(x => x.ScheduledSessionId)
            .NotNull()
            .WithMessage("Recenzija mora biti vezana za završeni termin u kojem ste sudjelovali.")
            .GreaterThan(0)
            .WithMessage("Recenzija mora biti vezana za završeni termin u kojem ste sudjelovali.");
        RuleFor(x => x.RatingStars).InclusiveBetween((byte)1, (byte)5);
        RuleFor(x => x.Comment).MaximumLength(2000).When(x => !string.IsNullOrWhiteSpace(x.Comment));
    }
}

