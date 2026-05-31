namespace ArenaBook.Application.Contracts.Payments;

public sealed class CreatePayPalCoinOrderRequest
{
    public decimal CoinsToPurchase { get; set; }

    public string? IdempotencyKey { get; set; }

    public string ReturnUrl { get; set; } = string.Empty;

    public string CancelUrl { get; set; } = string.Empty;
}

