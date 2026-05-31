namespace ArenaBook.Application.Contracts.Payments;

public sealed class ConfirmStripeSandboxCoinPurchaseRequest
{
    public decimal CoinsToPurchase { get; set; }

    public string? IdempotencyKey { get; set; }
}

