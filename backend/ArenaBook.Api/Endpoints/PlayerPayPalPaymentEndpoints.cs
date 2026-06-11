using System.Security.Claims;
using ArenaBook.Application.Abstractions.Payments;
using ArenaBook.Application.Authorization;
using ArenaBook.Application.Contracts.Payments;

namespace ArenaBook.Api.Endpoints;

public static class PlayerPayPalPaymentEndpoints
{
    public static WebApplication MapPlayerPayPalPaymentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/me/payments/paypal")
            .RequireAuthorization(AuthPolicies.PlayerApp)
            .WithTags("Payments - Me (PayPal sandbox)");

        group.MapPost("/create-order", CreateOrderAsync);
        group.MapPost("/capture", CaptureAsync);
        group.MapPost("/confirm-sandbox", ConfirmSandboxAsync);
        group.MapPost("/refund", RefundAsync);

        return app;
    }

    private static string RequireUserId(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id))
            throw new InvalidOperationException();
        return id;
    }

    private static Task<CreatePayPalCoinOrderResponse> CreateOrderAsync(
        ClaimsPrincipal user,
        IPayPalCoinSandboxService service,
        CreatePayPalCoinOrderRequest request,
        CancellationToken cancellationToken)
    {
        return service.CreateCoinPurchaseOrderAsync(RequireUserId(user), request, cancellationToken);
    }

    private static async Task<IResult> CaptureAsync(
        ClaimsPrincipal user,
        IPayPalCoinSandboxService service,
        CapturePayPalOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CaptureCoinPurchaseOrderAsync(RequireUserId(user), request, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> ConfirmSandboxAsync(
        ClaimsPrincipal user,
        IPayPalCoinSandboxService service,
        ConfirmPayPalSandboxPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.ConfirmSandboxPurchaseAsync(RequireUserId(user), request, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> RefundAsync(
        ClaimsPrincipal user,
        IPayPalCoinSandboxService service,
        RefundCoinPurchaseRequest request,
        CancellationToken cancellationToken)
    {
        await service.RefundCoinPurchaseAsync(RequireUserId(user), request.ExternalPaymentRecordId, cancellationToken);
        return Results.NoContent();
    }
}

