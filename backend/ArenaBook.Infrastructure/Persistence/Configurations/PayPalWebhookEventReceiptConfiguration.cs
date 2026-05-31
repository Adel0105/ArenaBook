using ArenaBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaBook.Infrastructure.Persistence.Configurations;

public sealed class PayPalWebhookEventReceiptConfiguration : IEntityTypeConfiguration<PayPalWebhookEventReceipt>
{
    public void Configure(EntityTypeBuilder<PayPalWebhookEventReceipt> builder)
    {
        builder.ToTable("PayPalWebhookEventReceipts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PayPalEventId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ReceivedUtc).IsRequired();
        builder.HasIndex(x => x.PayPalEventId).IsUnique();
    }
}

