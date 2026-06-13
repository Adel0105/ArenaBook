using ArenaBook.Domain.Entities;
using ArenaBook.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaBook.Infrastructure.Persistence.Configurations;

public sealed class ScheduledSessionConfiguration : IEntityTypeConfiguration<ScheduledSession>
{
    public void Configure(EntityTypeBuilder<ScheduledSession> builder)
    {
        builder.ToTable("ScheduledSessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrganizerUserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.StartUtc).IsRequired();
        builder.Property(x => x.EndUtc).IsRequired();
        builder.Property(x => x.PriceTotalCoins).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.PricePerParticipantCoins).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.InviteCode).HasMaxLength(32);
        builder.Property(x => x.CreatedUtc).IsRequired();
        builder.HasOne(x => x.Hall)
            .WithMany(x => x.ScheduledSessions)
            .HasForeignKey(x => x.HallId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SessionKind)
            .WithMany(x => x.ScheduledSessions)
            .HasForeignKey(x => x.SessionKindId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SessionLifecycleStatus)
            .WithMany(x => x.ScheduledSessions)
            .HasForeignKey(x => x.SessionLifecycleStatusId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.OrganizerUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.InviteCode)
            .IsUnique()
            .HasFilter("[InviteCode] IS NOT NULL");
    }
}

