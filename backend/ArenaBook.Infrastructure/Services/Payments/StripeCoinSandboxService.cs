using System.Globalization;
using System.Text.Json;
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
using Stripe;

namespace ArenaBook.Infrastructure.Services.Payments;

public sealed class StripeCoinSandboxService : IStripeCoinSandboxService
{
    private const string CoinsPerBamKey = "Platform.Coins.CoinsPerBam";
    private const string MinCoinsKey = "Platform.Coins.MinPurchaseCoins";
    private const string MaxCoinsKey = "Platform.Coins.MaxPurchaseCoins";
    private const string PurchaseCurrency = "BAM";
    private const string StripeSandboxReturnUrl = "https://arenabook.local/stripe/return";

    private readonly ArenaBookDbContext _db;
    private readonly StripeOptions _stripe;
    private readonly PayPalOptions _paypal;
    private readonly IRabbitMqEventPublisher _rabbit;
    private readonly ICoinPurchaseFinalizer _coinPurchaseFinalizer;
    private readonly IValidator<CreateCoinPurchaseIntentRequest> _createValidator;
    private readonly IValidator<ConfirmStripeSandboxCoinPurchaseRequest> _confirmCoinPurchaseValidator;
    private readonly IHostEnvironment _env;
    private readonly ILogger<StripeCoinSandboxService> _logger;

    public StripeCoinSandboxService(
        ArenaBookDbContext db,
        IOptions<StripeOptions> stripeOptions,
        IOptions<PayPalOptions> paypalOptions,
        IRabbitMqEventPublisher rabbit,
        ICoinPurchaseFinalizer coinPurchaseFinalizer,
        IValidator<CreateCoinPurchaseIntentRequest> createValidator,
        IValidator<ConfirmStripeSandboxCoinPurchaseRequest> confirmCoinPurchaseValidator,
        IHostEnvironment env,
        ILogger<StripeCoinSandboxService> logger)
    {
        _db = db;
        _stripe = stripeOptions.Value;
        _paypal = paypalOptions.Value;
        _rabbit = rabbit;
        _coinPurchaseFinalizer = coinPurchaseFinalizer;
        _createValidator = createValidator;
        _confirmCoinPurchaseValidator = confirmCoinPurchaseValidator;
        _env = env;
        _logger = logger;
        StripeConfiguration.ApiKey = _stripe.SecretKey;
    }

    public async Task<CreateCoinPurchaseIntentResponse> CreateCoinPurchaseIntentAsync(
        string userId,
        CreateCoinPurchaseIntentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_stripe.SecretKey))
            throw new InvalidOperationException("Stripe nije konfigurisan (Stripe__SecretKey).");

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
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
            var existing = await _db.ExternalPaymentRecords
                .FirstOrDefaultAsync(
                    x => x.UserId == userId && x.IdempotencyKey == idempotencyKey && x.Provider == "Stripe",
                    cancellationToken);
            if (existing is not null)
            {
                if (existing.PaymentProcessingStatusId == completedId)
                    throw new ConflictException("Ova idempotentna uplata je već završena.");

                if (!string.IsNullOrEmpty(existing.ExternalReference))
                {
                    var svc = new PaymentIntentService();
                    var pi = await svc.GetAsync(existing.ExternalReference, cancellationToken: cancellationToken);
                    return new CreateCoinPurchaseIntentResponse
                    {
                        ClientSecret = pi.ClientSecret,
                        PaymentIntentId = pi.Id,
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

        var amountMinor = (long)Math.Round(money * 100m, 0, MidpointRounding.AwayFromZero);
        if (amountMinor < 50)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Minimalni iznos za Stripe je ispod dozvoljenog praga.",
                new Dictionary<string, string[]>());

        var piOptions = new PaymentIntentCreateOptions
        {
            Amount = amountMinor,
            Currency = PurchaseCurrency.ToLowerInvariant(),
            PaymentMethodTypes = ["card"],
            Metadata = new Dictionary<string, string>
            {
                ["userId"] = userId,
                ["coins"] = request.CoinsToPurchase.ToString(CultureInfo.InvariantCulture),
                ["purpose"] = "COIN_PURCHASE",
            },
        };

        var piService = new PaymentIntentService();
        var stripeRequestOptions = new RequestOptions();
        if (!string.IsNullOrEmpty(idempotencyKey))
            stripeRequestOptions.IdempotencyKey = "coin-intent-" + userId + "-" + idempotencyKey;
        else
            stripeRequestOptions.IdempotencyKey = "coin-intent-" + userId + "-" + Guid.NewGuid().ToString("N");

        PaymentIntent intent;
        try
        {
            intent = await piService.CreateAsync(piOptions, stripeRequestOptions, cancellationToken);
        }
        catch (StripeException ex)
        {
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Stripe nije prihvatio zahtjev.",
                new Dictionary<string, string[]> { { "stripe", new[] { ex.Message } } });
        }

        var row = new ExternalPaymentRecord
        {
            UserId = userId,
            PurposeCode = "COIN_PURCHASE",
            Provider = "Stripe",
            AmountMoney = money,
            Currency = PurchaseCurrency,
            PaymentProcessingStatusId = pendingId,
            ExternalReference = intent.Id,
            IdempotencyKey = idempotencyKey,
            CoinsPurchased = request.CoinsToPurchase,
            CreatedUtc = DateTime.UtcNow,
        };
        _db.ExternalPaymentRecords.Add(row);
        await _db.SaveChangesAsync(cancellationToken);

        return new CreateCoinPurchaseIntentResponse
        {
            ClientSecret = intent.ClientSecret,
            PaymentIntentId = intent.Id,
            ExternalPaymentRecordId = row.Id,
            AmountMoney = money,
            Currency = PurchaseCurrency,
            CoinsToPurchase = request.CoinsToPurchase,
        };
    }

    public async Task RefundCoinPurchaseAsync(
        string userId,
        int externalPaymentRecordId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_stripe.SecretKey))
            throw new InvalidOperationException("Stripe nije konfigurisan (Stripe__SecretKey).");

        var completedId = await StatusIdByCodeAsync("COMPLETED", cancellationToken);
        var cancelledId = await StatusIdByCodeAsync("CANCELLED", cancellationToken);

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        var payment = await _db.ExternalPaymentRecords
            .FirstOrDefaultAsync(
                x => x.Id == externalPaymentRecordId && x.UserId == userId && x.PurposeCode == "COIN_PURCHASE",
                cancellationToken);
        if (payment is null)
            throw new NotFoundException("Uplata nije pronađena.");

        if (payment.PaymentProcessingStatusId != completedId)
            throw new ConflictException("Samo završene uplate mogu biti refundirane.");

        if (string.IsNullOrEmpty(payment.ExternalReference))
            throw new ConflictException("Nedostaje Stripe referenca.");

        var wallet = await _db.UserCoinWallets.FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
        if (wallet is null)
            throw new ConflictException("Novčanik ne postoji.");

        var coins = payment.CoinsPurchased;
        if (wallet.BalanceCoins < coins)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Nedovoljno koina na računu za refund (koini su vjerovatno potrošeni).",
                new Dictionary<string, string[]>());

        var refundService = new RefundService();
        try
        {
            await refundService.CreateAsync(
                new RefundCreateOptions { PaymentIntent = payment.ExternalReference },
                cancellationToken: cancellationToken);
        }
        catch (StripeException ex)
        {
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Stripe refund nije uspio.",
                new Dictionary<string, string[]> { { "stripe", new[] { ex.Message } } });
        }

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

    public async Task HandleStripeWebhookAsync(
        string rawJson,
        string stripeSignatureHeader,
        CancellationToken cancellationToken = default)
    {
        Event stripeEvent;
        if (!string.IsNullOrWhiteSpace(_stripe.WebhookSecret))
        {
            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    rawJson,
                    stripeSignatureHeader,
                    _stripe.WebhookSecret,
                    throwOnApiVersionMismatch: false);
            }
            catch (StripeException ex)
            {
                throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                    "Neispravan Stripe webhook.",
                    new Dictionary<string, string[]> { { "stripe", new[] { ex.Message } } });
            }
        }
        else if (_env.IsDevelopment())
        {
            try
            {
                stripeEvent = EventUtility.ParseEvent(rawJson, throwOnApiVersionMismatch: false);
            }
            catch (StripeException ex)
            {
                throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                    "Neispravan Stripe payload.",
                    new Dictionary<string, string[]> { { "stripe", new[] { ex.Message } } });
            }
        }
        else
        {
            throw new InvalidOperationException("Stripe:WebhookSecret je obavezan izvan Development okruženja.");
        }

        if (stripeEvent.Type == "payment_intent.succeeded")
        {
            await HandlePaymentIntentSucceededAsync(stripeEvent, cancellationToken);
            return;
        }

        if (stripeEvent.Type == "payment_intent.payment_failed")
        {
            await HandlePaymentIntentFailedAsync(stripeEvent, cancellationToken);
        }
    }

    private async Task HandlePaymentIntentFailedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not PaymentIntent pi)
            return;

        var failedId = await StatusIdByCodeAsync("FAILED", cancellationToken);
        var payment = await _db.ExternalPaymentRecords
            .FirstOrDefaultAsync(x => x.ExternalReference == pi.Id, cancellationToken);
        if (payment is null)
            return;

        payment.PaymentProcessingStatusId = failedId;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task HandlePaymentIntentSucceededAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not PaymentIntent pi)
            return;

        var payment = await _db.ExternalPaymentRecords.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ExternalReference == pi.Id, cancellationToken);
        if (payment is null
            && pi.Metadata != null
            && pi.Metadata.TryGetValue("externalPaymentRecordId", out var extIdStr)
            && int.TryParse(extIdStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var extId))
        {
            payment = await _db.ExternalPaymentRecords.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == extId, cancellationToken);
        }

        if (payment is null)
            return;

        await _coinPurchaseFinalizer.FinalizeCoinPurchaseAsync(
            payment.Id,
            pi.Id,
            stripeWebhookEventId: stripeEvent.Id,
            cancellationToken: cancellationToken);
    }

    public async Task ConfirmSandboxPaymentAsync(
        string userId,
        ConfirmStripeSandboxPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!PaymentSandboxPolicy.IsEnabled(_env, _stripe, _paypal))
            throw new InvalidOperationException("Stripe sandbox potvrda nije omogućena u ovom okruženju.");

        if (string.IsNullOrWhiteSpace(_stripe.SecretKey))
            throw new InvalidOperationException("Stripe nije konfigurisan (Stripe__SecretKey).");

        var paymentIntentId = request.PaymentIntentId.Trim();
        if (string.IsNullOrEmpty(paymentIntentId))
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "PaymentIntentId je obavezan.",
                new Dictionary<string, string[]> { ["paymentIntentId"] = ["PaymentIntentId je obavezan."] });

        var pendingId = await StatusIdByCodeAsync("PENDING", cancellationToken);
        var payment = await _db.ExternalPaymentRecords
            .FirstOrDefaultAsync(
                x => x.UserId == userId && x.ExternalReference == paymentIntentId && x.Provider == "Stripe",
                cancellationToken);
        if (payment is null)
            throw new NotFoundException("Uplata nije pronađena.");
        if (payment.PaymentProcessingStatusId != pendingId)
            throw new ConflictException("Uplata nije u statusu čekanja.");

        var piService = new PaymentIntentService();
        try
        {
            await piService.ConfirmAsync(
                paymentIntentId,
                new PaymentIntentConfirmOptions
                {
                    PaymentMethod = "pm_card_visa",
                    ReturnUrl = StripeSandboxReturnUrl,
                },
                cancellationToken: cancellationToken);
        }
        catch (StripeException ex)
        {
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Stripe potvrda nije uspjela.",
                new Dictionary<string, string[]> { ["stripe"] = [ex.Message] });
        }

        var pi = await piService.GetAsync(paymentIntentId, cancellationToken: cancellationToken);
        if (!string.Equals(pi.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("Plaćanje nije u statusu succeeded.");

        var synthetic = new Event
        {
            Id = "sandbox-" + Guid.NewGuid().ToString("N"),
            Type = "payment_intent.succeeded",
            Data = new EventData { Object = pi },
        };
        await HandlePaymentIntentSucceededAsync(synthetic, cancellationToken);
    }

    public async Task<CoinPurchaseResultResponse> ConfirmSandboxCoinPurchaseAsync(
        string userId,
        ConfirmStripeSandboxCoinPurchaseRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!PaymentSandboxPolicy.IsEnabled(_env, _stripe, _paypal))
            throw new InvalidOperationException("Stripe sandbox kupovina nije omogućena u ovom okruženju.");

        if (string.IsNullOrWhiteSpace(_stripe.SecretKey))
            throw new InvalidOperationException("Stripe nije konfigurisan (Stripe__SecretKey).");

        var validation = await _confirmCoinPurchaseValidator.ValidateAsync(request, cancellationToken);
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
                x => x.UserId == userId && x.IdempotencyKey == idempotencyKey && x.Provider == "Stripe",
                cancellationToken);
            if (existing is not null && existing.PaymentProcessingStatusId == completedId)
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
        }

        var money = Math.Round(request.CoinsToPurchase / rate, 2, MidpointRounding.AwayFromZero);
        if (money < 0.5m)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Iznos u valuti je premali za ovaj broj koina.",
                new Dictionary<string, string[]>());

        var amountMinor = (long)Math.Round(money * 100m, 0, MidpointRounding.AwayFromZero);
        if (amountMinor < 50)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Minimalni iznos za Stripe je ispod dozvoljenog praga.",
                new Dictionary<string, string[]>());

        var row = new ExternalPaymentRecord
        {
            UserId = userId,
            PurposeCode = "COIN_PURCHASE",
            Provider = "Stripe",
            AmountMoney = money,
            Currency = PurchaseCurrency,
            PaymentProcessingStatusId = pendingId,
            IdempotencyKey = idempotencyKey,
            CoinsPurchased = request.CoinsToPurchase,
            CreatedUtc = DateTime.UtcNow,
        };
        _db.ExternalPaymentRecords.Add(row);
        await _db.SaveChangesAsync(cancellationToken);

        var piOptions = new PaymentIntentCreateOptions
        {
            Amount = amountMinor,
            Currency = PurchaseCurrency.ToLowerInvariant(),
            PaymentMethodTypes = ["card"],
            PaymentMethod = "pm_card_visa",
            Confirm = true,
            Metadata = new Dictionary<string, string>
            {
                ["userId"] = userId,
                ["coins"] = request.CoinsToPurchase.ToString(CultureInfo.InvariantCulture),
                ["purpose"] = "COIN_PURCHASE",
                ["externalPaymentRecordId"] = row.Id.ToString(CultureInfo.InvariantCulture),
            },
        };

        var piService = new PaymentIntentService();
        var stripeRequestOptions = new RequestOptions
        {
            IdempotencyKey = !string.IsNullOrEmpty(idempotencyKey)
                ? "stripe-sandbox-" + userId + "-" + idempotencyKey
                : "stripe-sandbox-" + userId + "-" + Guid.NewGuid().ToString("N"),
        };

        PaymentIntent intent;
        try
        {
            intent = await piService.CreateAsync(piOptions, stripeRequestOptions, cancellationToken);
        }
        catch (StripeException ex)
        {
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Stripe nije prihvatio zahtjev.",
                new Dictionary<string, string[]> { ["stripe"] = [ex.Message] });
        }

        row.ExternalReference = intent.Id;
        await _db.SaveChangesAsync(cancellationToken);

        if (!string.Equals(intent.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
            throw new ConflictException($"Plaćanje nije uspjelo (status: {intent.Status}).");

        var finalized = await _coinPurchaseFinalizer.FinalizeCoinPurchaseAsync(
            row.Id,
            intent.Id,
            cancellationToken: cancellationToken);
        if (!finalized.Credited && !finalized.AlreadyCompleted)
            throw new ConflictException("Uplata nije mogla biti završena.");

        return new CoinPurchaseResultResponse
        {
            BalanceCoins = finalized.BalanceCoins,
            CoinsPurchased = finalized.CoinsPurchased,
        };
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

