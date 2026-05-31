namespace ArenaBook.Application.Contracts.Payments;

public sealed class CapturePayPalOrderRequest
{
    public string PayPalOrderId { get; set; } = string.Empty;
}

