namespace ArenaBook.Application.Contracts.Payments;

public sealed class CreatePayPalCoinOrderResponse
{
    public string PayPalOrderId { get; set; } = string.Empty;

    public string ApprovalUrl { get; set; } = string.Empty;

    public int ExternalPaymentRecordId { get; set; }

    public decimal AmountMoney { get; set; }

    public string Currency { get; set; } = string.Empty;

    public decimal CoinsToPurchase { get; set; }
}

