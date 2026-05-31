using ArenaBook.Domain.Entities;
using ArenaBook.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ArenaBook.Infrastructure.Persistence;

public sealed class ArenaBookDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public ArenaBookDbContext(DbContextOptions<ArenaBookDbContext> options)
        : base(options)
    {
    }

    public DbSet<Country> Countries => Set<Country>();

    public DbSet<City> Cities => Set<City>();

    public DbSet<EquipmentType> EquipmentTypes => Set<EquipmentType>();

    public DbSet<SessionKind> SessionKinds => Set<SessionKind>();

    public DbSet<SessionLifecycleStatus> SessionLifecycleStatuses => Set<SessionLifecycleStatus>();

    public DbSet<PaymentProcessingStatus> PaymentProcessingStatuses => Set<PaymentProcessingStatus>();

    public DbSet<Hall> Halls => Set<Hall>();

    public DbSet<HallPhoto> HallPhotos => Set<HallPhoto>();

    public DbSet<HallEquipment> HallEquipments => Set<HallEquipment>();

    public DbSet<ScheduledSession> ScheduledSessions => Set<ScheduledSession>();

    public DbSet<ScheduledSessionParticipant> ScheduledSessionParticipants => Set<ScheduledSessionParticipant>();

    public DbSet<ScheduledSessionAuditEntry> ScheduledSessionAuditEntries => Set<ScheduledSessionAuditEntry>();

    public DbSet<UserCoinWallet> UserCoinWallets => Set<UserCoinWallet>();

    public DbSet<CoinLedgerEntry> CoinLedgerEntries => Set<CoinLedgerEntry>();

    public DbSet<ExternalPaymentRecord> ExternalPaymentRecords => Set<ExternalPaymentRecord>();

    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();

    public DbSet<StripeWebhookEventReceipt> StripeWebhookEventReceipts => Set<StripeWebhookEventReceipt>();

    public DbSet<PayPalWebhookEventReceipt> PayPalWebhookEventReceipts => Set<PayPalWebhookEventReceipt>();

    public DbSet<HallReview> HallReviews => Set<HallReview>();

    public DbSet<HallReaction> HallReactions => Set<HallReaction>();

    public DbSet<PlatformSettingEntry> PlatformSettingEntries => Set<PlatformSettingEntry>();

    public DbSet<RevokedAccessToken> RevokedAccessTokens => Set<RevokedAccessToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ArenaBookDbContext).Assembly);
    }
}

