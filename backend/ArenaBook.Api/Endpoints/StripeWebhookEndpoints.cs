using ArenaBook.Application.Abstractions.Payments;
using System.Text;

namespace ArenaBook.Api.Endpoints;

public static class StripeWebhookEndpoints
{
    public static WebApplication MapStripeWebhookEndpoints(this WebApplication app)
    {
        app.MapPost("/api/webhooks/stripe", HandleStripeAsync)
            .AllowAnonymous()
            .DisableAntiforgery()
            .WithTags("Integration - Stripe");

        return app;
    }

    private static async Task<IResult> HandleStripeAsync(
        HttpRequest request,
        IStripeCoinSandboxService service,
        CancellationToken cancellationToken)
    {
        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        var json = await reader.ReadToEndAsync(cancellationToken);
        if (request.Body.CanSeek)
            request.Body.Position = 0;

        var sig = request.Headers["Stripe-Signature"].ToString();
        await service.HandleStripeWebhookAsync(json, sig, cancellationToken);
        return Results.Ok();
    }
}

