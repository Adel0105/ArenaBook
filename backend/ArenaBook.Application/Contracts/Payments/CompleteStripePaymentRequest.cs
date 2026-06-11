namespace ArenaBook.Application.Contracts.Payments;

public sealed class CompleteStripePaymentRequest
{
    public string PaymentIntentId { get; set; } = string.Empty;
}
