namespace ArenaBook.Application.Options;

public sealed class PayPalOptions
{
    public const string SectionName = "PayPal";

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string BaseApiUrl { get; set; } = "https://api-m.sandbox.paypal.com";

    public string WebhookId { get; set; } = string.Empty;
}

