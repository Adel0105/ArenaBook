namespace ArenaBook.Domain.Entities;

public sealed class StripeWebhookEventReceipt
{
    public int Id { get; set; }

    public string StripeEventId { get; set; } = string.Empty;

    public DateTime ReceivedUtc { get; set; }
}

