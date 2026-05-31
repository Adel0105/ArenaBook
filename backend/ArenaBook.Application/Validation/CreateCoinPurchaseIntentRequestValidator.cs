using ArenaBook.Application.Contracts.Payments;
using FluentValidation;

namespace ArenaBook.Application.Validation;

public sealed class CreateCoinPurchaseIntentRequestValidator : AbstractValidator<CreateCoinPurchaseIntentRequest>
{
    public CreateCoinPurchaseIntentRequestValidator()
    {
        RuleFor(x => x.CoinsToPurchase).GreaterThan(0).LessThanOrEqualTo(500_000m);
        RuleFor(x => x.IdempotencyKey).MaximumLength(128).When(x => !string.IsNullOrWhiteSpace(x.IdempotencyKey));
    }
}

