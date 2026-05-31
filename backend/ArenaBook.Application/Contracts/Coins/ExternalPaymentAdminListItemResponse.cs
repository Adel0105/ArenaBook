namespace ArenaBook.Application.Contracts.Coins;

public sealed class ExternalPaymentAdminListItemResponse
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string? UserEmail { get; set; }

    public string PurposeCode { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public decimal AmountMoney { get; set; }

    public string Currency { get; set; } = string.Empty;

    public int PaymentProcessingStatusId { get; set; }

    public string? PaymentStatusCode { get; set; }

    public string? ExternalReference { get; set; }

    public decimal CoinsPurchased { get; set; }

    public DateTime CreatedUtc { get; set; }
}

