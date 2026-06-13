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
            .Must(u => string.IsNullOrWhiteSpace(u) || IsAllowedPayPalRedirectUrl(u))
            .WithMessage("ReturnUrl mora biti apsolutni URL (http, https ili arenabook deep link).");
        RuleFor(x => x.CancelUrl).MaximumLength(2048)
            .Must(u => string.IsNullOrWhiteSpace(u) || IsAllowedPayPalRedirectUrl(u))
            .WithMessage("CancelUrl mora biti apsolutni URL (http, https ili arenabook deep link).");
    }

    private static bool IsAllowedPayPalRedirectUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        return string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Scheme, "arenabook", StringComparison.OrdinalIgnoreCase);
    }
}

