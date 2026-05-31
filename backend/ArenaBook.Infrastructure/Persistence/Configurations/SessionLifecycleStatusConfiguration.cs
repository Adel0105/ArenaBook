using ArenaBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaBook.Infrastructure.Persistence.Configurations;

public sealed class SessionLifecycleStatusConfiguration : IEntityTypeConfiguration<SessionLifecycleStatus>
{
    public void Configure(EntityTypeBuilder<SessionLifecycleStatus> builder)
    {
        builder.ToTable("SessionLifecycleStatuses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasData(
            new SessionLifecycleStatus { Id = 1, Code = "PENDING", DisplayName = "Na čekanju" },
            new SessionLifecycleStatus { Id = 2, Code = "CONFIRMED", DisplayName = "Potvrđeno" },
            new SessionLifecycleStatus { Id = 3, Code = "CANCELLED", DisplayName = "Otkazano" },
            new SessionLifecycleStatus { Id = 4, Code = "COMPLETED", DisplayName = "Završeno" });
    }
}

