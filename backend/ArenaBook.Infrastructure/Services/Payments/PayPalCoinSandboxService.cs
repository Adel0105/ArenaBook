using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ArenaBook.Application.Abstractions.Coins;
using ArenaBook.Application.Abstractions.Messaging;
using ArenaBook.Application.Abstractions.Payments;
using ArenaBook.Application.Common.Exceptions;
using ArenaBook.Application.Contracts.Payments;
using ArenaBook.Application.Options;
using ArenaBook.Domain;
using ArenaBook.Domain.Entities;
using ArenaBook.Domain.Messaging;
using ArenaBook.Infrastructure.Persistence;
using ArenaBook.Infrastructure.Validation;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArenaBook.Infrastructure.Services.Payments;

public sealed class PayPalCoinSandboxService : IPayPalCoinSandboxService
{
    private const string ProviderName = "PayPal";
    private const string CoinsPerBamKey = "Platform.Coins.CoinsPerBam";
    private const string MinCoinsKey = "Platform.Coins.MinPurchaseCoins";
    private const string MaxCoinsKey = "Platform.Coins.MaxPurchaseCoins";
    /// <summary>PayPal sandbox ne podržava BAM; USD je standard za test checkout.</summary>
    private const string PurchaseCurrency = "USD";
    private const string HttpClientName = "paypal";

    private readonly ArenaBookDbContext _db;
    private readonly PayPalOptions _paypal;
    private readonly StripeOptions _stripe;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IRabbitMqEventPublisher _rabbit;
    private readonly ICoinPurchaseFinalizer _coinPurchaseFinalizer;
    private readonly IValidator<CreatePayPalCoinOrderRequest> _createOrderValidator;
    private readonly IValidator<CapturePayPalOrderRequest> _captureValidator;
    private readonly IValidator<ConfirmPayPalSandboxPaymentRequest> _confirmSandboxValidator;
    private readonly IHostEnvironment _env;
    private readonly ILogger<PayPalCoinSandboxService> _logger;

    private static readonly SemaphoreSlim TokenSemaphore = new(1, 1);
    private static string? _cachedAccessToken;
    private static DateTimeOffset _accessTokenExpiresUtc;

    public PayPalCoinSandboxService(
        ArenaBookDbContext db,
        IOptions<PayPalOptions> paypalOptions,
        IOptions<StripeOptions> stripeOptions,
        IHttpClientFactory httpFactory,
        IRabbitMqEventPublisher rabbit,
        ICoinPurchaseFinalizer coinPurchaseFinalizer,
        IValidator<CreatePayPalCoinOrderRequest> createOrderValidator,
        IValidator<CapturePayPalOrderRequest> captureValidator,
        IValidator<ConfirmPayPalSandboxPaymentRequest> confirmSandboxValidator,
        IHostEnvironment env,
        ILogger<PayPalCoinSandboxService> logger)
    {
        _db = db;
        _paypal = paypalOptions.Value;
        _stripe = stripeOptions.Value;
        _httpFactory = httpFactory;
        _rabbit = rabbit;
        _coinPurchaseFinalizer = coinPurchaseFinalizer;
        _createOrderValidator = createOrderValidator;
        _captureValidator = captureValidator;
        _confirmSandboxValidator = confirmSandboxValidator;
        _env = env;
        _logger = logger;
    }

    public async Task<CreatePayPalCoinOrderResponse> CreateCoinPurchaseOrderAsync(
        string userId,
        CreatePayPalCoinOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePayPalConfigured();

        var validation = await _createOrderValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Validacija nije prošla.",
                validation.ToErrorDictionary());

        var rate = await GetPlatformDecimalAsync(CoinsPerBamKey, 10m, cancellationToken);
        if (rate <= 0)
            throw new InvalidOperationException("Platform.Coins.CoinsPerBam mora biti veći od nule.");

        var minCoins = await GetPlatformDecimalAsync(MinCoinsKey, 10m, cancellationToken);
        var maxCoins = await GetPlatformDecimalAsync(MaxCoinsKey, 100_000m, cancellationToken);
        if (request.CoinsToPurchase < minCoins || request.CoinsToPurchase > maxCoins)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                $"Broj koina mora biti između {minCoins.ToString(CultureInfo.InvariantCulture)} i {maxCoins.ToString(CultureInfo.InvariantCulture)}.",
                new Dictionary<string, string[]>());

        var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? null
            : request.IdempotencyKey.Trim();

        var pendingId = await StatusIdByCodeAsync("PENDING", cancellationToken);
        var completedId = await StatusIdByCodeAsync("COMPLETED", cancellationToken);

        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var existing = await _db.ExternalPaymentRecords.FirstOrDefaultAsync(
                x => x.UserId == userId && x.IdempotencyKey == idempotencyKey && x.Provider == ProviderName,
                cancellationToken);
            if (existing is not null)
            {
                if (existing.PaymentProcessingStatusId == completedId)
                    throw new ConflictException("Ova idempotentna uplata je već završena.");

                if (!string.IsNullOrEmpty(existing.ExternalReference))
                {
                    using var doc = await GetPayPalOrderJsonAsync(existing.ExternalReference, cancellationToken);
                    var approval = ExtractApprovalUrl(doc);
                    return new CreatePayPalCoinOrderResponse
                    {
                        PayPalOrderId = existing.ExternalReference,
                        ApprovalUrl = approval,
                        ExternalPaymentRecordId = existing.Id,
                        AmountMoney = existing.AmountMoney,
                        Currency = existing.Currency,
                        CoinsToPurchase = existing.CoinsPurchased,
                    };
                }
            }
        }

        var money = Math.Round(request.CoinsToPurchase / rate, 2, MidpointRounding.AwayFromZero);
        if (money < 0.5m)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Iznos u valuti je premali za ovaj broj koina.",
                new Dictionary<string, string[]>());

        var row = new ExternalPaymentRecord
        {
            UserId = userId,
            PurposeCode = "COIN_PURCHASE",
            Provider = ProviderName,
            AmountMoney = money,
            Currency = PurchaseCurrency,
            PaymentProcessingStatusId = pendingId,
            ExternalReference = null,
            IdempotencyKey = idempotencyKey,
            CoinsPurchased = request.CoinsToPurchase,
            CreatedUtc = DateTime.UtcNow,
        };
        _db.ExternalPaymentRecords.Add(row);
        await _db.SaveChangesAsync(cancellationToken);

        var purchaseUnit = BuildDigitalGoodsPurchaseUnit(money, row.Id, request.CoinsToPurchase);

        var orderBody = new Dictionary<string, object?>
        {
            ["intent"] = "CAPTURE",
            ["purchase_units"] = new List<Dictionary<string, object?>> { purchaseUnit },
            ["application_context"] = BuildApplicationContext(request.ReturnUrl, request.CancelUrl),
        };

        var payPalRequestId = !string.IsNullOrEmpty(idempotencyKey)
            ? "paypal-order-" + userId + "-" + idempotencyKey
            : "paypal-order-" + userId + "-" + Guid.NewGuid().ToString("N");

        using var orderDoc = await PostPayPalJsonAsync(
            HttpMethod.Post,
            "v2/checkout/orders",
            orderBody,
            payPalRequestId,
            cancellationToken);

        var root = orderDoc.RootElement;
        var orderId = root.GetProperty("id").GetString();
        if (string.IsNullOrEmpty(orderId))
            throw new InvalidOperationException("PayPal nije vratio ID narudžbe.");

        var approvalUrl = ExtractApprovalUrl(orderDoc);

        row.ExternalReference = orderId;
        await _db.SaveChangesAsync(cancellationToken);

        return new CreatePayPalCoinOrderResponse
        {
            PayPalOrderId = orderId,
            ApprovalUrl = approvalUrl,
            ExternalPaymentRecordId = row.Id,
            AmountMoney = money,
            Currency = PurchaseCurrency,
            CoinsToPurchase = request.CoinsToPurchase,
        };
    }

    public async Task<CoinPurchaseResultResponse> CaptureCoinPurchaseOrderAsync(
        string userId,
        CapturePayPalOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePayPalConfigured();

        var validation = await _captureValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Validacija nije prošla.",
                validation.ToErrorDictionary());

        var orderId = request.PayPalOrderId.Trim();
        var pendingId = await StatusIdByCodeAsync("PENDING", cancellationToken);

        var payment = await _db.ExternalPaymentRecords.FirstOrDefaultAsync(
            x => x.UserId == userId
                 && x.Provider == ProviderName
                 && x.ExternalReference == orderId
                 && x.PaymentProcessingStatusId == pendingId,
            cancellationToken);
        if (payment is null)
            throw new NotFoundException("PayPal narudžba nije pronađena ili nije na čekanju.");

        var captureInfo = await TryCaptureApprovedOrderAsync(orderId, cancellationToken);

        var captureStatus = captureInfo.Status;
        if (string.IsNullOrWhiteSpace(captureStatus))
        {
            captureStatus = await GetPayPalCaptureStatusAsync(
                captureInfo.CaptureId,
                cancellationToken);
        }

        if (!IsPayPalCaptureCompleted(captureStatus))
        {
            await RejectIncompletePayPalCaptureAsync(payment, captureStatus, cancellationToken);
            throw new ConflictException($"PayPal capture nije uspio (status: {captureStatus}).");
        }

        var finalized = await FinalizePayPalCaptureAsync(
            payment.Id,
            captureInfo.CaptureId,
            null,
            cancellationToken);
        if (!finalized.Credited && !finalized.AlreadyCompleted)
            throw new ConflictException("Uplata je već obrađena.");

        return new CoinPurchaseResultResponse
        {
            BalanceCoins = finalized.BalanceCoins,
            CoinsPurchased = finalized.CoinsPurchased,
        };
    }

    public async Task<CoinPurchaseResultResponse> ConfirmSandboxPurchaseAsync(
        string userId,
        ConfirmPayPalSandboxPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!PaymentSandboxPolicy.IsEnabled(_env, _stripe, _paypal))
            throw new InvalidOperationException("PayPal sandbox simulacija nije omogućena u ovom okruženju.");

        var validation = await _confirmSandboxValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Validacija nije prošla.",
                validation.ToErrorDictionary());

        var rate = await GetPlatformDecimalAsync(CoinsPerBamKey, 10m, cancellationToken);
        if (rate <= 0)
            throw new InvalidOperationException("Platform.Coins.CoinsPerBam mora biti veći od nule.");

        var minCoins = await GetPlatformDecimalAsync(MinCoinsKey, 10m, cancellationToken);
        var maxCoins = await GetPlatformDecimalAsync(MaxCoinsKey, 100_000m, cancellationToken);
        if (request.CoinsToPurchase < minCoins || request.CoinsToPurchase > maxCoins)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                $"Broj koina mora biti između {minCoins.ToString(CultureInfo.InvariantCulture)} i {maxCoins.ToString(CultureInfo.InvariantCulture)}.",
                new Dictionary<string, string[]>());

        var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? null
            : request.IdempotencyKey.Trim();

        var pendingId = await StatusIdByCodeAsync("PENDING", cancellationToken);
        var completedId = await StatusIdByCodeAsync("COMPLETED", cancellationToken);

        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var existing = await _db.ExternalPaymentRecords.FirstOrDefaultAsync(
                x => x.UserId == userId && x.IdempotencyKey == idempotencyKey && x.Provider == ProviderName,
                cancellationToken);
            if (existing is not null)
            {
                if (existing.PaymentProcessingStatusId == completedId)
                {
                    var balance = await _db.UserCoinWallets.AsNoTracking()
                        .Where(w => w.UserId == userId)
                        .Select(w => w.BalanceCoins)
                        .FirstOrDefaultAsync(cancellationToken);
                    return new CoinPurchaseResultResponse
                    {
                        BalanceCoins = balance,
                        CoinsPurchased = existing.CoinsPurchased,
                    };
                }

                if (existing.PaymentProcessingStatusId == pendingId)
                {
                    var finalizedExisting = await FinalizePayPalCaptureAsync(
                        existing.Id,
                        existing.ExternalReference ?? "SANDBOX-PAYPAL-" + existing.Id,
                        null,
                        cancellationToken);
                    if (!finalizedExisting.Credited && !finalizedExisting.AlreadyCompleted)
                        throw new ConflictException("Uplata je već obrađena.");
                    return new CoinPurchaseResultResponse
                    {
                        BalanceCoins = finalizedExisting.BalanceCoins,
                        CoinsPurchased = finalizedExisting.CoinsPurchased,
                    };
                }
            }
        }

        var money = Math.Round(request.CoinsToPurchase / rate, 2, MidpointRounding.AwayFromZero);
        if (money < 0.5m)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Iznos u valuti je premali za ovaj broj koina.",
                new Dictionary<string, string[]>());

        var row = new ExternalPaymentRecord
        {
            UserId = userId,
            PurposeCode = "COIN_PURCHASE",
            Provider = ProviderName,
            AmountMoney = money,
            Currency = PurchaseCurrency,
            PaymentProcessingStatusId = pendingId,
            ExternalReference = "SANDBOX-PAYPAL-" + Guid.NewGuid().ToString("N"),
            IdempotencyKey = idempotencyKey,
            CoinsPurchased = request.CoinsToPurchase,
            CreatedUtc = DateTime.UtcNow,
        };
        _db.ExternalPaymentRecords.Add(row);
        await _db.SaveChangesAsync(cancellationToken);

        var finalized = await FinalizePayPalCaptureAsync(
            row.Id,
            row.ExternalReference,
            null,
            cancellationToken);
        if (!finalized.Credited && !finalized.AlreadyCompleted)
            throw new ConflictException("Uplata je već obrađena.");

        return new CoinPurchaseResultResponse
        {
            BalanceCoins = finalized.BalanceCoins,
            CoinsPurchased = finalized.CoinsPurchased,
        };
    }

    public async Task RefundCoinPurchaseAsync(
        string userId,
        int externalPaymentRecordId,
        CancellationToken cancellationToken = default)
    {
        EnsurePayPalConfigured();

        var completedId = await StatusIdByCodeAsync("COMPLETED", cancellationToken);
        var cancelledId = await StatusIdByCodeAsync("CANCELLED", cancellationToken);

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        var payment = await _db.ExternalPaymentRecords.FirstOrDefaultAsync(
            x => x.Id == externalPaymentRecordId && x.UserId == userId && x.Provider == ProviderName && x.PurposeCode == "COIN_PURCHASE",
            cancellationToken);
        if (payment is null)
            throw new NotFoundException("Uplata nije pronađena.");

        if (payment.PaymentProcessingStatusId != completedId)
            throw new ConflictException("Samo završene uplate mogu biti refundirane.");

        if (string.IsNullOrEmpty(payment.ExternalReference))
            throw new ConflictException("Nedostaje PayPal capture referenca.");

        var wallet = await _db.UserCoinWallets.FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
        if (wallet is null)
            throw new ConflictException("Novčanik ne postoji.");

        var coins = payment.CoinsPurchased;
        if (wallet.BalanceCoins < coins)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Nedovoljno koina na računu za refund (koini su vjerovatno potrošeni).",
                new Dictionary<string, string[]>());

        var captureId = payment.ExternalReference;
        await PostPayPalJsonAsync(
            HttpMethod.Post,
            "v2/payments/captures/" + Uri.EscapeDataString(captureId) + "/refund",
            new Dictionary<string, object?>(),
            "paypal-refund-" + captureId + "-" + Guid.NewGuid().ToString("N"),
            cancellationToken);

        wallet.BalanceCoins -= coins;
        wallet.UpdatedUtc = DateTime.UtcNow;
        wallet.LedgerEntries.Add(new CoinLedgerEntry
        {
            AmountCoins = -coins,
            BalanceAfter = wallet.BalanceCoins,
            ReasonCode = CoinLedgerReasonCodes.CoinPurchaseRefund,
            RelatedScheduledSessionId = null,
            CreatedUtc = DateTime.UtcNow,
        });

        payment.PaymentProcessingStatusId = cancelledId;
        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    public async Task HandlePayPalWebhookAsync(
        string rawJson,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken = default)
    {
        EnsurePayPalConfigured();

        if (!string.IsNullOrWhiteSpace(_paypal.WebhookId))
        {
            await VerifyWebhookSignatureAsync(rawJson, headers, cancellationToken);
        }
        else if (!_env.IsDevelopment())
        {
            throw new InvalidOperationException("PayPal:WebhookId je obavezan izvan Development okruženja.");
        }

        using var ev = JsonDocument.Parse(rawJson);
        var root = ev.RootElement;
        var eventType = root.TryGetProperty("event_type", out var et) ? et.GetString() : null;
        if (!string.Equals(eventType, "PAYMENT.CAPTURE.COMPLETED", StringComparison.Ordinal))
            return;

        if (!root.TryGetProperty("resource", out var resource))
            return;

        var captureId = resource.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        if (string.IsNullOrEmpty(captureId))
            return;

        var captureStatus = resource.TryGetProperty("status", out var statusEl)
            ? statusEl.GetString()
            : null;
        if (!IsPayPalCaptureCompleted(captureStatus))
            return;

        var externalPaymentId = TryParseCustomId(resource);
        if (externalPaymentId is null)
            return;

        var eventId = root.TryGetProperty("id", out var eid) ? eid.GetString() : null;
        if (string.IsNullOrEmpty(eventId))
            return;

        await FinalizePayPalCaptureAsync(externalPaymentId.Value, captureId, eventId, cancellationToken);
    }

    private Task<CoinPurchaseFinalizeResult> FinalizePayPalCaptureAsync(
        int externalPaymentRecordId,
        string captureId,
        string? payPalWebhookEventId,
        CancellationToken cancellationToken)
    {
        return _coinPurchaseFinalizer.FinalizeCoinPurchaseAsync(
            externalPaymentRecordId,
            captureId,
            payPalWebhookEventId: payPalWebhookEventId,
            cancellationToken: cancellationToken);
    }

    private static int? TryParseCustomId(JsonElement resource)
    {
        if (!resource.TryGetProperty("custom_id", out var c))
            return null;
        var s = c.GetString();
        return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : null;
    }

    private async Task VerifyWebhookSignatureAsync(
        string rawJson,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken)
    {
        static string H(IReadOnlyDictionary<string, string> h, string canonical)
        {
            foreach (var kv in h)
            {
                if (string.Equals(kv.Key, canonical, StringComparison.OrdinalIgnoreCase))
                    return kv.Value ?? string.Empty;
            }

            return string.Empty;
        }

        var verifyRoot = new JsonObject
        {
            ["transmission_id"] = H(headers, "PAYPAL-TRANSMISSION-ID"),
            ["transmission_time"] = H(headers, "PAYPAL-TRANSMISSION-TIME"),
            ["cert_url"] = H(headers, "PAYPAL-CERT-URL"),
            ["auth_algo"] = H(headers, "PAYPAL-AUTH-ALGO"),
            ["transmission_sig"] = H(headers, "PAYPAL-TRANSMISSION-SIG"),
            ["webhook_id"] = _paypal.WebhookId,
            ["webhook_event"] = JsonNode.Parse(rawJson)!,
        };

        var verifyJson = verifyRoot.ToJsonString();
        using var verifyDoc = await PostPayPalRawJsonAsync(
            "v1/notifications/verify-webhook-signature",
            verifyJson,
            "paypal-verify-" + Guid.NewGuid().ToString("N"),
            cancellationToken);

        var status = verifyDoc.RootElement.TryGetProperty("verification_status", out var st)
            ? st.GetString()
            : null;
        if (!string.Equals(status, "SUCCESS", StringComparison.Ordinal))
        {
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "PayPal webhook verifikacija nije uspjela.",
                new Dictionary<string, string[]>());
        }
    }

    private void EnsurePayPalConfigured()
    {
        if (string.IsNullOrWhiteSpace(_paypal.ClientId) || string.IsNullOrWhiteSpace(_paypal.ClientSecret))
            throw new InvalidOperationException("PayPal nije konfigurisan (PayPal__ClientId, PayPal__ClientSecret).");
    }

    private async Task<JsonDocument> GetPayPalOrderJsonAsync(string orderId, CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        var client = _httpFactory.CreateClient(HttpClientName);
        using var req = new HttpRequestMessage(HttpMethod.Get, "v2/checkout/orders/" + Uri.EscapeDataString(orderId));
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var resp = await client.SendAsync(req, cancellationToken);
        var text = await resp.Content.ReadAsStringAsync(cancellationToken);
        if (!resp.IsSuccessStatusCode)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "PayPal narudžba nije dostupna.",
                new Dictionary<string, string[]> { { "paypal", new[] { FormatPayPalError(text) } } });

        return JsonDocument.Parse(text);
    }

    private async Task<JsonDocument> PostPayPalJsonAsync(
        HttpMethod method,
        string relativePath,
        object body,
        string? payPalRequestId,
        CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        var client = _httpFactory.CreateClient(HttpClientName);
        using var req = new HttpRequestMessage(method, relativePath);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrEmpty(payPalRequestId))
            req.Headers.TryAddWithoutValidation("PayPal-Request-Id", payPalRequestId);

        var json = JsonSerializer.Serialize(body);
        req.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var resp = await client.SendAsync(req, cancellationToken);
        var text = await resp.Content.ReadAsStringAsync(cancellationToken);
        if (!resp.IsSuccessStatusCode)
        {
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "PayPal API greška.",
                new Dictionary<string, string[]> { { "paypal", new[] { FormatPayPalError(text) } } });
        }

        return string.IsNullOrWhiteSpace(text)
            ? JsonDocument.Parse("{}")
            : JsonDocument.Parse(text);
    }

    private async Task<PayPalCaptureInfo> TryCaptureApprovedOrderAsync(
        string orderId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await CaptureApprovedOrderAsync(orderId, cancellationToken);
        }
        catch (ArenaBook.Application.Common.Exceptions.ValidationException ex)
            when (IsPayPalComplianceViolation(ex) && PaymentSandboxPolicy.IsEnabled(_env, _stripe, _paypal))
        {
            if (!await IsPayPalOrderApprovedByBuyerAsync(orderId, cancellationToken))
                throw;

            _logger.LogWarning(
                "PayPal COMPLIANCE_VIOLATION na capture za order {OrderId}; sandbox fallback — novcici se dodaju nakon odobrenja.",
                orderId);

            return new PayPalCaptureInfo("SANDBOX-COMPLIANCE-" + orderId, "COMPLETED");
        }
    }

    private async Task<bool> IsPayPalOrderApprovedByBuyerAsync(
        string orderId,
        CancellationToken cancellationToken)
    {
        using var orderDoc = await GetPayPalOrderJsonAsync(orderId, cancellationToken);
        var status = orderDoc.RootElement.TryGetProperty("status", out var st)
            ? st.GetString()
            : null;

        return string.Equals(status, "APPROVED", StringComparison.OrdinalIgnoreCase)
               || string.Equals(status, "COMPLETED", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPayPalComplianceViolation(
        ArenaBook.Application.Common.Exceptions.ValidationException ex)
    {
        if (!ex.Errors.TryGetValue("paypal", out var messages))
            return false;

        return messages.Any(m =>
            m.Contains("COMPLIANCE_VIOLATION", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<PayPalCaptureInfo> CaptureApprovedOrderAsync(
        string orderId,
        CancellationToken cancellationToken)
    {
        await WaitForPayPalOrderReadyAsync(orderId, cancellationToken);

        using var orderDoc = await GetPayPalOrderJsonAsync(orderId, cancellationToken);
        var status = orderDoc.RootElement.TryGetProperty("status", out var st)
            ? st.GetString()
            : null;

        if (string.Equals(status, "COMPLETED", StringComparison.OrdinalIgnoreCase))
        {
            var existing = ExtractCaptureFromCaptureResponse(orderDoc);
            if (existing is not null && !string.IsNullOrEmpty(existing.Value.CaptureId))
                return existing.Value;
        }

        if (!string.Equals(status, "APPROVED", StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException(
                $"PayPal narudžba nije spremna za capture (status: {status ?? "nepoznat"}). "
                + "Završite odobrenje u PayPal pregledniku i pokušajte ponovo.");
        }

        using var captureDoc = await PostPayPalJsonAsync(
            HttpMethod.Post,
            "v2/checkout/orders/" + Uri.EscapeDataString(orderId) + "/capture",
            new Dictionary<string, object?>(),
            "paypal-capture-" + orderId + "-" + Guid.NewGuid().ToString("N"),
            cancellationToken);

        var captureInfo = ExtractCaptureFromCaptureResponse(captureDoc);
        if (captureInfo is null || string.IsNullOrEmpty(captureInfo.Value.CaptureId))
            throw new InvalidOperationException("PayPal capture odgovor ne sadrži ID uplate.");

        return captureInfo.Value;
    }

    private async Task WaitForPayPalOrderReadyAsync(string orderId, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            using var orderDoc = await GetPayPalOrderJsonAsync(orderId, cancellationToken);
            var status = orderDoc.RootElement.TryGetProperty("status", out var st)
                ? st.GetString()
                : null;

            if (string.Equals(status, "APPROVED", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "COMPLETED", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (string.Equals(status, "VOIDED", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictException("PayPal narudžba je otkazana prije capture-a.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(750), cancellationToken);
        }
    }

    private static Dictionary<string, object?> BuildDigitalGoodsPurchaseUnit(
        decimal money,
        int paymentRecordId,
        decimal coins)
    {
        var amountText = money.ToString("F2", CultureInfo.InvariantCulture);
        return new Dictionary<string, object?>
        {
            ["reference_id"] = "coin_" + paymentRecordId,
            ["custom_id"] = paymentRecordId.ToString(CultureInfo.InvariantCulture),
            ["description"] = "ArenaBook novcici",
            ["amount"] = new Dictionary<string, object?>
            {
                ["currency_code"] = PurchaseCurrency,
                ["value"] = amountText,
                ["breakdown"] = new Dictionary<string, object?>
                {
                    ["item_total"] = new Dictionary<string, object?>
                    {
                        ["currency_code"] = PurchaseCurrency,
                        ["value"] = amountText,
                    },
                },
            },
            ["items"] = new List<Dictionary<string, object?>>
            {
                new()
                {
                    ["name"] = "ArenaBook novcici",
                    ["description"] = $"Kupovina {coins.ToString("0.##", CultureInfo.InvariantCulture)} novcica",
                    ["quantity"] = "1",
                    ["category"] = "DIGITAL_GOODS",
                    ["unit_amount"] = new Dictionary<string, object?>
                    {
                        ["currency_code"] = PurchaseCurrency,
                        ["value"] = amountText,
                    },
                },
            },
        };
    }

    private static Dictionary<string, object?> BuildApplicationContext(string? returnUrl, string? cancelUrl) =>
        new()
        {
            ["return_url"] = string.IsNullOrWhiteSpace(returnUrl)
                ? "https://arenabook.local/paypal/return"
                : returnUrl.Trim(),
            ["cancel_url"] = string.IsNullOrWhiteSpace(cancelUrl)
                ? "https://arenabook.local/paypal/cancel"
                : cancelUrl.Trim(),
            ["brand_name"] = "ArenaBook",
            ["landing_page"] = "NO_PREFERENCE",
            ["shipping_preference"] = "NO_SHIPPING",
            ["user_action"] = "PAY_NOW",
        };

    private static string FormatPayPalError(string responseText)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseText);
            var root = doc.RootElement;
            if (root.TryGetProperty("details", out var details)
                && details.ValueKind == JsonValueKind.Array
                && details.GetArrayLength() > 0)
            {
                var first = details[0];
                var issue = first.TryGetProperty("issue", out var issueEl)
                    ? issueEl.GetString()
                    : null;
                if (string.Equals(issue, "COMPLIANCE_VIOLATION", StringComparison.OrdinalIgnoreCase))
                {
                    return "PayPal sandbox je odbio uplatu (COMPLIANCE_VIOLATION). "
                           + "Prijavite se Personal sandbox kupcem (ne Business računom), po mogućnosti US država. "
                           + "Kreirajte novi Personal sandbox account u developer.paypal.com ako treba.";
                }

                var description = first.TryGetProperty("description", out var descEl)
                    ? descEl.GetString()
                    : null;
                if (!string.IsNullOrWhiteSpace(description))
                    return description;
            }

            if (root.TryGetProperty("message", out var messageEl))
            {
                var message = messageEl.GetString();
                if (!string.IsNullOrWhiteSpace(message))
                    return message;
            }
        }
        catch (JsonException)
        {
            // fallback
        }

        return responseText.Length > 400 ? responseText[..400] + "…" : responseText;
    }

    private async Task<JsonDocument> PostPayPalRawJsonAsync(
        string relativePath,
        string jsonBody,
        string? payPalRequestId,
        CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        var client = _httpFactory.CreateClient(HttpClientName);
        using var req = new HttpRequestMessage(HttpMethod.Post, relativePath);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrEmpty(payPalRequestId))
            req.Headers.TryAddWithoutValidation("PayPal-Request-Id", payPalRequestId);

        req.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        using var resp = await client.SendAsync(req, cancellationToken);
        var text = await resp.Content.ReadAsStringAsync(cancellationToken);
        if (!resp.IsSuccessStatusCode)
        {
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "PayPal API greška.",
                new Dictionary<string, string[]> { { "paypal", new[] { FormatPayPalError(text) } } });
        }

        return string.IsNullOrWhiteSpace(text)
            ? JsonDocument.Parse("{}")
            : JsonDocument.Parse(text);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        await TokenSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTime.UtcNow < _accessTokenExpiresUtc.AddMinutes(-2))
                return _cachedAccessToken!;

            var client = _httpFactory.CreateClient(HttpClientName);
            using var req = new HttpRequestMessage(HttpMethod.Post, "v1/oauth2/token");
            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes(_paypal.ClientId + ":" + _paypal.ClientSecret));
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
            req.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var resp = await client.SendAsync(req, cancellationToken);
            var text = await resp.Content.ReadAsStringAsync(cancellationToken);
            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException("PayPal OAuth nije uspio: " + text);

            using var doc = JsonDocument.Parse(text);
            var access = doc.RootElement.GetProperty("access_token").GetString();
            var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var ex) ? ex.GetInt32() : 3600;
            if (string.IsNullOrEmpty(access))
                throw new InvalidOperationException("PayPal OAuth nije vratio token.");

            _cachedAccessToken = access;
            _accessTokenExpiresUtc = DateTime.UtcNow.AddSeconds(Math.Max(60, expiresIn));
            return access;
        }
        finally
        {
            TokenSemaphore.Release();
        }
    }

    private static string ExtractApprovalUrl(JsonDocument doc)
    {
        if (!doc.RootElement.TryGetProperty("links", out var links) || links.ValueKind != JsonValueKind.Array)
            return string.Empty;

        foreach (var link in links.EnumerateArray())
        {
            if (!link.TryGetProperty("rel", out var rel) || !link.TryGetProperty("href", out var href))
                continue;
            if (string.Equals(rel.GetString(), "approve", StringComparison.OrdinalIgnoreCase))
                return href.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private readonly record struct PayPalCaptureInfo(string CaptureId, string Status);

    private static bool IsPayPalCaptureCompleted(string? status) =>
        string.Equals(status, "COMPLETED", StringComparison.OrdinalIgnoreCase);

    private async Task RejectIncompletePayPalCaptureAsync(
        ExternalPaymentRecord payment,
        string captureStatus,
        CancellationToken cancellationToken)
    {
        if (string.Equals(captureStatus, "PENDING", StringComparison.OrdinalIgnoreCase))
            return;

        var failedId = await StatusIdByCodeAsync("FAILED", cancellationToken);
        if (payment.PaymentProcessingStatusId != failedId)
        {
            payment.PaymentProcessingStatusId = failedId;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<string> GetPayPalCaptureStatusAsync(
        string captureId,
        CancellationToken cancellationToken)
    {
        using var doc = await GetPayPalCaptureJsonAsync(captureId, cancellationToken);
        return doc.RootElement.TryGetProperty("status", out var st)
            ? st.GetString() ?? string.Empty
            : string.Empty;
    }

    private async Task<JsonDocument> GetPayPalCaptureJsonAsync(
        string captureId,
        CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        var client = _httpFactory.CreateClient(HttpClientName);
        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            "v2/payments/captures/" + Uri.EscapeDataString(captureId));
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var resp = await client.SendAsync(req, cancellationToken);
        var text = await resp.Content.ReadAsStringAsync(cancellationToken);
        if (!resp.IsSuccessStatusCode)
        {
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "PayPal capture nije dostupan.",
                new Dictionary<string, string[]> { { "paypal", new[] { FormatPayPalError(text) } } });
        }

        return JsonDocument.Parse(text);
    }

    private static PayPalCaptureInfo? ExtractCaptureFromCaptureResponse(JsonDocument doc)
    {
        if (!doc.RootElement.TryGetProperty("purchase_units", out var units) || units.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var unit in units.EnumerateArray())
        {
            if (!unit.TryGetProperty("payments", out var payments))
                continue;
            if (!payments.TryGetProperty("captures", out var captures) || captures.ValueKind != JsonValueKind.Array)
                continue;
            foreach (var cap in captures.EnumerateArray())
            {
                if (!cap.TryGetProperty("id", out var id))
                    continue;
                var captureId = id.GetString();
                if (string.IsNullOrEmpty(captureId))
                    continue;
                var status = cap.TryGetProperty("status", out var st) ? st.GetString() ?? string.Empty : string.Empty;
                return new PayPalCaptureInfo(captureId, status);
            }
        }

        return null;
    }

    private async Task<int> StatusIdByCodeAsync(string code, CancellationToken cancellationToken)
    {
        return await _db.PaymentProcessingStatuses.AsNoTracking()
            .Where(x => x.Code == code)
            .Select(x => x.Id)
            .FirstAsync(cancellationToken);
    }

    private async Task<decimal> GetPlatformDecimalAsync(string key, decimal fallback, CancellationToken cancellationToken)
    {
        var v = await _db.PlatformSettingEntries.AsNoTracking()
            .Where(x => x.SettingKey == key)
            .Select(x => x.SettingValue)
            .FirstOrDefaultAsync(cancellationToken);
        return decimal.TryParse(
            v,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var d)
            ? d
            : fallback;
    }
}

