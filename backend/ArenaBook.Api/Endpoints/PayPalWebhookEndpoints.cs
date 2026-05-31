using ArenaBook.Application.Abstractions.Payments;
using System.Text;

namespace ArenaBook.Api.Endpoints;

public static class PayPalWebhookEndpoints
{
    public static WebApplication MapPayPalWebhookEndpoints(this WebApplication app)
    {
        app.MapPost("/api/webhooks/paypal", HandlePayPalAsync)
            .AllowAnonymous()
            .DisableAntiforgery()
            .WithTags("Integration - PayPal");

        return app;
    }

    private static async Task<IResult> HandlePayPalAsync(
        HttpRequest request,
        IPayPalCoinSandboxService service,
        CancellationToken cancellationToken)
    {
        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        var json = await reader.ReadToEndAsync(cancellationToken);
        if (request.Body.CanSeek)
            request.Body.Position = 0;

        var headers = request.Headers.ToDictionary(
            h => h.Key,
            h => h.Value.FirstOrDefault() ?? string.Empty,
            StringComparer.OrdinalIgnoreCase);

        await service.HandlePayPalWebhookAsync(json, headers, cancellationToken);
        return Results.Ok();
    }
}

