using ArenaBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaBook.Infrastructure.Persistence.Configurations;

public sealed class StripeWebhookEventReceiptConfiguration : IEntityTypeConfiguration<StripeWebhookEventReceipt>
{
    public void Configure(EntityTypeBuilder<StripeWebhookEventReceipt> builder)
    {
        builder.ToTable("StripeWebhookEventReceipts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StripeEventId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ReceivedUtc).IsRequired();
        builder.HasIndex(x => x.StripeEventId).IsUnique();
    }
}

