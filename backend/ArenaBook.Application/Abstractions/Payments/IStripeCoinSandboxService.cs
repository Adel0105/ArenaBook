using ArenaBook.Application.Contracts.Payments;

namespace ArenaBook.Application.Abstractions.Payments;

public interface IStripeCoinSandboxService
{
    Task<CreateCoinPurchaseIntentResponse> CreateCoinPurchaseIntentAsync(
        string userId,
        CreateCoinPurchaseIntentRequest request,
        CancellationToken cancellationToken = default);

    Task RefundCoinPurchaseAsync(string userId, int externalPaymentRecordId, CancellationToken cancellationToken = default);

    Task HandleStripeWebhookAsync(string rawJson, string stripeSignatureHeader, CancellationToken cancellationToken = default);

    Task ConfirmSandboxPaymentAsync(
        string userId,
        ConfirmStripeSandboxPaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<CoinPurchaseResultResponse> ConfirmSandboxCoinPurchaseAsync(
        string userId,
        ConfirmStripeSandboxCoinPurchaseRequest request,
        CancellationToken cancellationToken = default);

    Task<CoinPurchaseResultResponse> CompletePaymentAsync(
        string userId,
        CompleteStripePaymentRequest request,
        CancellationToken cancellationToken = default);
}

