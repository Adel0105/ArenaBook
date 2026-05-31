using ArenaBook.Application.Contracts.Payments;
using FluentValidation;

namespace ArenaBook.Application.Validation;

public sealed class CreatePayPalCoinOrderRequestValidator : AbstractValidator<CreatePayPalCoinOrderRequest>
{
    public CreatePayPalCoinOrderRequestValidator()
    {
        RuleFor(x => x.CoinsToPurchase).GreaterThan(0).LessThanOrEqualTo(500_000m);
        RuleFor(x => x.IdempotencyKey).MaximumLength(128).When(x => !string.IsNullOrWhiteSpace(x.IdempotencyKey));
        RuleFor(x => x.ReturnUrl).MaximumLength(2048)
            .Must(u => string.IsNullOrWhiteSpace(u) || (Uri.TryCreate(u, UriKind.Absolute, out var z)
                && (z.Scheme == Uri.UriSchemeHttps || z.Scheme == Uri.UriSchemeHttp)))
            .WithMessage("ReturnUrl mora biti apsolutni URL.");
        RuleFor(x => x.CancelUrl).MaximumLength(2048)
            .Must(u => string.IsNullOrWhiteSpace(u) || (Uri.TryCreate(u, UriKind.Absolute, out var z)
                && (z.Scheme == Uri.UriSchemeHttps || z.Scheme == Uri.UriSchemeHttp)))
            .WithMessage("CancelUrl mora biti apsolutni URL.");
    }
}

