using ArenaBook.Domain.Entities;
using ArenaBook.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaBook.Infrastructure.Persistence.Configurations;

public sealed class ExternalPaymentRecordConfiguration : IEntityTypeConfiguration<ExternalPaymentRecord>
{
    public void Configure(EntityTypeBuilder<ExternalPaymentRecord> builder)
    {
        builder.ToTable("ExternalPaymentRecords");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.PurposeCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Provider).HasMaxLength(32).IsRequired();
        builder.Property(x => x.AmountMoney).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.ExternalReference).HasMaxLength(256);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(128);
        builder.Property(x => x.CoinsPurchased).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.CreatedUtc).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.IdempotencyKey, x.Provider }).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");
        builder.HasOne(x => x.PaymentProcessingStatus)
            .WithMany(x => x.ExternalPayments)
            .HasForeignKey(x => x.PaymentProcessingStatusId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

