using ArenaBook.Application.Options;
using Microsoft.Extensions.Hosting;

namespace ArenaBook.Infrastructure.Services.Payments;

internal static class PaymentSandboxPolicy
{
    public static bool IsEnabled(IHostEnvironment env, StripeOptions stripe, PayPalOptions paypal)
    {
        if (env.IsDevelopment())
            return true;

        if (!string.IsNullOrWhiteSpace(stripe.SecretKey)
            && stripe.SecretKey.StartsWith("sk_test_", StringComparison.Ordinal))
            return true;

        return !string.IsNullOrWhiteSpace(paypal.BaseApiUrl)
               && paypal.BaseApiUrl.Contains("sandbox", StringComparison.OrdinalIgnoreCase);
    }
}

