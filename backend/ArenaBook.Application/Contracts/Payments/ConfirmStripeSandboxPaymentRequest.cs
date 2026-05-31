namespace ArenaBook.Application.Contracts.Payments;

public sealed class ConfirmStripeSandboxPaymentRequest
{
    public string PaymentIntentId { get; set; } = string.Empty;
}

