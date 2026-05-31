namespace ArenaBook.Application.Abstractions.Coins;

public sealed class CoinPurchaseFinalizeResult
{
    public bool Credited { get; init; }

    public bool AlreadyCompleted { get; init; }

    public decimal BalanceCoins { get; init; }

    public decimal CoinsPurchased { get; init; }

    public string UserId { get; init; } = string.Empty;
}

public interface ICoinPurchaseFinalizer
{
    Task<CoinPurchaseFinalizeResult> FinalizeCoinPurchaseAsync(
        int externalPaymentRecordId,
        string? externalReference,
        string? stripeWebhookEventId = null,
        string? payPalWebhookEventId = null,
        CancellationToken cancellationToken = default);
}

