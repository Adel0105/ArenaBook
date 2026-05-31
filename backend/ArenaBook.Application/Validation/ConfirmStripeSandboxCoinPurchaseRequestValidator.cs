using ArenaBook.Application.Contracts.Payments;
using FluentValidation;

namespace ArenaBook.Application.Validation;

public sealed class ConfirmStripeSandboxCoinPurchaseRequestValidator
    : AbstractValidator<ConfirmStripeSandboxCoinPurchaseRequest>
{
    public ConfirmStripeSandboxCoinPurchaseRequestValidator()
    {
        RuleFor(x => x.CoinsToPurchase).GreaterThan(0).LessThanOrEqualTo(500_000m);
        RuleFor(x => x.IdempotencyKey).MaximumLength(128).When(x => !string.IsNullOrWhiteSpace(x.IdempotencyKey));
    }
}

