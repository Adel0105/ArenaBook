using System.Security.Claims;
using ArenaBook.Application.Abstractions.Payments;
using ArenaBook.Application.Authorization;
using ArenaBook.Application.Contracts.Payments;

namespace ArenaBook.Api.Endpoints;

public static class PlayerStripePaymentEndpoints
{
    public static WebApplication MapPlayerStripePaymentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/me/payments/stripe")
            .RequireAuthorization(AuthPolicies.PlayerApp)
            .WithTags("Payments - Me (Stripe sandbox)");

        group.MapPost("/create-intent", CreateIntentAsync);
        group.MapPost("/complete", CompleteAsync);
        group.MapPost("/confirm-sandbox", ConfirmSandboxAsync);
        group.MapPost("/purchase-sandbox", PurchaseSandboxAsync);
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

    private static Task<CreateCoinPurchaseIntentResponse> CreateIntentAsync(
        ClaimsPrincipal user,
        IStripeCoinSandboxService service,
        CreateCoinPurchaseIntentRequest request,
        CancellationToken cancellationToken)
    {
        return service.CreateCoinPurchaseIntentAsync(RequireUserId(user), request, cancellationToken);
    }

    private static async Task<IResult> CompleteAsync(
        ClaimsPrincipal user,
        IStripeCoinSandboxService service,
        CompleteStripePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CompletePaymentAsync(RequireUserId(user), request, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> ConfirmSandboxAsync(
        ClaimsPrincipal user,
        IStripeCoinSandboxService service,
        ConfirmStripeSandboxPaymentRequest request,
        CancellationToken cancellationToken)
    {
        await service.ConfirmSandboxPaymentAsync(RequireUserId(user), request, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> PurchaseSandboxAsync(
        ClaimsPrincipal user,
        IStripeCoinSandboxService service,
        ConfirmStripeSandboxCoinPurchaseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.ConfirmSandboxCoinPurchaseAsync(RequireUserId(user), request, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> RefundAsync(
        ClaimsPrincipal user,
        IStripeCoinSandboxService service,
        RefundCoinPurchaseRequest request,
        CancellationToken cancellationToken)
    {
        await service.RefundCoinPurchaseAsync(RequireUserId(user), request.ExternalPaymentRecordId, cancellationToken);
        return Results.NoContent();
    }
}

