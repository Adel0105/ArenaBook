using ArenaBook.Application.Contracts.Payments;

namespace ArenaBook.Application.Abstractions.Payments;

public interface IPayPalCoinSandboxService
{
    Task<CreatePayPalCoinOrderResponse> CreateCoinPurchaseOrderAsync(
        string userId,
        CreatePayPalCoinOrderRequest request,
        CancellationToken cancellationToken = default);

    Task CaptureCoinPurchaseOrderAsync(
        string userId,
        CapturePayPalOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<CoinPurchaseResultResponse> ConfirmSandboxPurchaseAsync(
        string userId,
        ConfirmPayPalSandboxPaymentRequest request,
        CancellationToken cancellationToken = default);

    Task RefundCoinPurchaseAsync(string userId, int externalPaymentRecordId, CancellationToken cancellationToken = default);

    Task HandlePayPalWebhookAsync(
        string rawJson,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken = default);
}

