namespace ArenaBook.Application.Contracts.Payments;

public sealed class ConfirmPayPalSandboxPaymentRequest
{
    public decimal CoinsToPurchase { get; set; }

    public string? IdempotencyKey { get; set; }
}

