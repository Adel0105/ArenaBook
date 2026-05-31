namespace ArenaBook.Domain.Entities;

public sealed class ExternalPaymentRecord
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string PurposeCode { get; set; } = "COIN_PURCHASE";

    public string Provider { get; set; } = string.Empty;

    public decimal AmountMoney { get; set; }

    public string Currency { get; set; } = "BAM";

    public int PaymentProcessingStatusId { get; set; }

    public PaymentProcessingStatus PaymentProcessingStatus { get; set; } = null!;

    public string? ExternalReference { get; set; }

    public string? IdempotencyKey { get; set; }

    public decimal CoinsPurchased { get; set; }

    public DateTime CreatedUtc { get; set; }
}

