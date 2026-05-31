namespace ArenaBook.Application.Contracts.Payments;

public sealed class RefundCoinPurchaseRequest
{
    public int ExternalPaymentRecordId { get; set; }
}

