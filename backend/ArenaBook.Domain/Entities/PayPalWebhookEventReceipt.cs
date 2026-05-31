namespace ArenaBook.Domain.Entities;

public sealed class PayPalWebhookEventReceipt
{
    public int Id { get; set; }

    public string PayPalEventId { get; set; } = string.Empty;

    public DateTime ReceivedUtc { get; set; }
}

