namespace ArenaBook.Application.Contracts.Payments;

public sealed class CreateCoinPurchaseIntentRequest
{
    public decimal CoinsToPurchase { get; set; }

    public string? IdempotencyKey { get; set; }
}

