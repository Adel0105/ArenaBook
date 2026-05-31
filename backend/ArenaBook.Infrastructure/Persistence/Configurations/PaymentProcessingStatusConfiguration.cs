using ArenaBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaBook.Infrastructure.Persistence.Configurations;

public sealed class PaymentProcessingStatusConfiguration : IEntityTypeConfiguration<PaymentProcessingStatus>
{
    public void Configure(EntityTypeBuilder<PaymentProcessingStatus> builder)
    {
        builder.ToTable("PaymentProcessingStatuses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasData(
            new PaymentProcessingStatus { Id = 1, Code = "PENDING", DisplayName = "Na čekanju" },
            new PaymentProcessingStatus { Id = 2, Code = "COMPLETED", DisplayName = "Uspješno" },
            new PaymentProcessingStatus { Id = 3, Code = "CANCELLED", DisplayName = "Otkazano" },
            new PaymentProcessingStatus { Id = 4, Code = "FAILED", DisplayName = "Neuspješno" });
    }
}

