using ArenaBook.Domain.Entities;
using ArenaBook.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaBook.Infrastructure.Persistence.Configurations;

public sealed class ScheduledSessionParticipantConfiguration : IEntityTypeConfiguration<ScheduledSessionParticipant>
{
    public void Configure(EntityTypeBuilder<ScheduledSessionParticipant> builder)
    {
        builder.ToTable("ScheduledSessionParticipants");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.JoinedUtc).IsRequired();
        builder.Property(x => x.CoinsPaid).HasPrecision(18, 2).IsRequired();
        builder.HasOne(x => x.ScheduledSession)
            .WithMany(x => x.Participants)
            .HasForeignKey(x => x.ScheduledSessionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ScheduledSessionId, x.UserId }).IsUnique();
    }
}

