using ArenaBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaBook.Infrastructure.Persistence.Configurations;

public sealed class CoinLedgerEntryConfiguration : IEntityTypeConfiguration<CoinLedgerEntry>
{
    public void Configure(EntityTypeBuilder<CoinLedgerEntry> builder)
    {
        builder.ToTable("CoinLedgerEntries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AmountCoins).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.BalanceAfter).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.ReasonCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.CreatedUtc).IsRequired();
        builder.HasOne(x => x.UserCoinWallet)
            .WithMany(x => x.LedgerEntries)
            .HasForeignKey(x => x.UserCoinWalletId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.RelatedScheduledSession)
            .WithMany(x => x.RelatedLedgerEntries)
            .HasForeignKey(x => x.RelatedScheduledSessionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

