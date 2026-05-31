using ArenaBook.Domain.Entities;
using ArenaBook.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaBook.Infrastructure.Persistence.Configurations;

public sealed class ScheduledSessionAuditEntryConfiguration : IEntityTypeConfiguration<ScheduledSessionAuditEntry>
{
    public void Configure(EntityTypeBuilder<ScheduledSessionAuditEntry> builder)
    {
        builder.ToTable("ScheduledSessionAuditEntries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ActorUserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(64).IsRequired();
        builder.Property(x => x.OccurredUtc).IsRequired();
        builder.Property(x => x.DetailsJson).HasMaxLength(4000);
        builder.HasIndex(x => x.SessionId);
        builder.HasIndex(x => x.OccurredUtc);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

