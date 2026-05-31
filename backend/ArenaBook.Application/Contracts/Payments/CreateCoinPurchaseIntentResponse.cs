namespace ArenaBook.Application.Contracts.Payments;

public sealed class CreateCoinPurchaseIntentResponse
{
    public string ClientSecret { get; set; } = string.Empty;

    public string PaymentIntentId { get; set; } = string.Empty;

    public int ExternalPaymentRecordId { get; set; }

    public decimal AmountMoney { get; set; }

    public string Currency { get; set; } = string.Empty;

    public decimal CoinsToPurchase { get; set; }
}

