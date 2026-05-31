using ArenaBook.Application.Contracts.Halls;
using FluentValidation;

namespace ArenaBook.Application.Validation;

public sealed class CreateHallReviewRequestValidator : AbstractValidator<CreateHallReviewRequest>
{
    public CreateHallReviewRequestValidator()
    {
        RuleFor(x => x.ScheduledSessionId)
            .GreaterThan(0)
            .When(x => x.ScheduledSessionId.HasValue);
        RuleFor(x => x.RatingStars).InclusiveBetween((byte)1, (byte)5);
        RuleFor(x => x.Comment).MaximumLength(2000).When(x => !string.IsNullOrWhiteSpace(x.Comment));
    }
}

