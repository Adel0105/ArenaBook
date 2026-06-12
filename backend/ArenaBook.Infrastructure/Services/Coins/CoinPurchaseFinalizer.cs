using ArenaBook.Application.Abstractions.Coins;
using ArenaBook.Application.Abstractions.Notifications;
using ArenaBook.Domain;
using ArenaBook.Domain.Entities;
using ArenaBook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ArenaBook.Infrastructure.Services.Coins;

public sealed class CoinPurchaseFinalizer : ICoinPurchaseFinalizer
{
    private readonly ArenaBookDbContext _db;
    private readonly IUserNotificationPublisher _notifications;
    private readonly ILogger<CoinPurchaseFinalizer> _logger;

    public CoinPurchaseFinalizer(
        ArenaBookDbContext db,
        IUserNotificationPublisher notifications,
        ILogger<CoinPurchaseFinalizer> logger)
    {
        _db = db;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<CoinPurchaseFinalizeResult> FinalizeCoinPurchaseAsync(
        int externalPaymentRecordId,
        string? externalReference,
        string? stripeWebhookEventId = null,
        string? payPalWebhookEventId = null,
        CancellationToken cancellationToken = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        if (!string.IsNullOrEmpty(stripeWebhookEventId))
        {
            try
            {
                _db.StripeWebhookEventReceipts.Add(new StripeWebhookEventReceipt
                {
                    StripeEventId = stripeWebhookEventId,
                    ReceivedUtc = DateTime.UtcNow,
                });
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                await tx.RollbackAsync(cancellationToken);
                return await ReadCompletedResultAsync(externalPaymentRecordId, cancellationToken);
            }
        }

        if (!string.IsNullOrEmpty(payPalWebhookEventId))
        {
            try
            {
                _db.PayPalWebhookEventReceipts.Add(new PayPalWebhookEventReceipt
                {
                    PayPalEventId = payPalWebhookEventId,
                    ReceivedUtc = DateTime.UtcNow,
                });
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                await tx.RollbackAsync(cancellationToken);
                return await ReadCompletedResultAsync(externalPaymentRecordId, cancellationToken);
            }
        }

        var pendingId = await StatusIdByCodeAsync("PENDING", cancellationToken);
        var completedId = await StatusIdByCodeAsync("COMPLETED", cancellationToken);

        var payment = await _db.ExternalPaymentRecords.FirstOrDefaultAsync(
            x => x.Id == externalPaymentRecordId,
            cancellationToken);

        if (payment is null || !string.Equals(payment.PurposeCode, "COIN_PURCHASE", StringComparison.Ordinal))
        {
            await tx.CommitAsync(cancellationToken);
            return new CoinPurchaseFinalizeResult();
        }

        if (payment.PaymentProcessingStatusId == completedId)
        {
            await tx.CommitAsync(cancellationToken);
            return await ReadCompletedResultAsync(payment, cancellationToken);
        }

        if (payment.PaymentProcessingStatusId != pendingId)
        {
            await tx.CommitAsync(cancellationToken);
            return new CoinPurchaseFinalizeResult();
        }

        var coins = payment.CoinsPurchased;
        if (coins <= 0)
        {
            await tx.CommitAsync(cancellationToken);
            return new CoinPurchaseFinalizeResult();
        }

        var wallet = await _db.UserCoinWallets.FirstOrDefaultAsync(
            w => w.UserId == payment.UserId,
            cancellationToken);
        if (wallet is null)
        {
            wallet = new UserCoinWallet
            {
                UserId = payment.UserId,
                BalanceCoins = 0,
                UpdatedUtc = DateTime.UtcNow,
            };
            _db.UserCoinWallets.Add(wallet);
            await _db.SaveChangesAsync(cancellationToken);
        }

        wallet.BalanceCoins += coins;
        wallet.UpdatedUtc = DateTime.UtcNow;
        wallet.LedgerEntries.Add(new CoinLedgerEntry
        {
            AmountCoins = coins,
            BalanceAfter = wallet.BalanceCoins,
            ReasonCode = CoinLedgerReasonCodes.CoinPurchaseCredit,
            RelatedScheduledSessionId = null,
            CreatedUtc = DateTime.UtcNow,
        });

        payment.PaymentProcessingStatusId = completedId;
        if (!string.IsNullOrWhiteSpace(externalReference))
            payment.ExternalReference = externalReference.Trim();

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        var isPayPal = string.Equals(payment.Provider, "PayPal", StringComparison.OrdinalIgnoreCase);
        await _notifications.TryPublishAsync(
            payment.UserId,
            isPayPal ? "Kupovina koina (PayPal)" : "Kupovina koina",
            isPayPal
                ? "PayPal uplata je potvrđena i stanje koina je ažurirano."
                : "Uplata je potvrđena i stanje koina je ažurirano.",
            isPayPal ? "coin_purchase_paypal_ok" : "coin_purchase_ok",
            cancellationToken);

        return new CoinPurchaseFinalizeResult
        {
            Credited = true,
            AlreadyCompleted = false,
            BalanceCoins = wallet.BalanceCoins,
            CoinsPurchased = coins,
            UserId = payment.UserId,
        };
    }

    private async Task<CoinPurchaseFinalizeResult> ReadCompletedResultAsync(
        int externalPaymentRecordId,
        CancellationToken cancellationToken)
    {
        var payment = await _db.ExternalPaymentRecords.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == externalPaymentRecordId, cancellationToken);
        if (payment is null)
            return new CoinPurchaseFinalizeResult();

        return await ReadCompletedResultAsync(payment, cancellationToken);
    }

    private async Task<CoinPurchaseFinalizeResult> ReadCompletedResultAsync(
        ExternalPaymentRecord payment,
        CancellationToken cancellationToken)
    {
        var balance = await _db.UserCoinWallets.AsNoTracking()
            .Where(w => w.UserId == payment.UserId)
            .Select(w => w.BalanceCoins)
            .FirstOrDefaultAsync(cancellationToken);

        return new CoinPurchaseFinalizeResult
        {
            Credited = false,
            AlreadyCompleted = true,
            BalanceCoins = balance,
            CoinsPurchased = payment.CoinsPurchased,
            UserId = payment.UserId,
        };
    }

    private async Task<int> StatusIdByCodeAsync(string code, CancellationToken cancellationToken)
    {
        return await _db.PaymentProcessingStatuses.AsNoTracking()
            .Where(x => x.Code == code)
            .Select(x => x.Id)
            .FirstAsync(cancellationToken);
    }
}

