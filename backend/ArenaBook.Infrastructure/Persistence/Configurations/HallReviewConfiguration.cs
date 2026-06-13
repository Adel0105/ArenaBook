using ArenaBook.Domain.Entities;
using ArenaBook.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaBook.Infrastructure.Persistence.Configurations;

public sealed class HallReviewConfiguration : IEntityTypeConfiguration<HallReview>
{
    public void Configure(EntityTypeBuilder<HallReview> builder)
    {
        builder.ToTable("HallReviews", t =>
            t.HasCheckConstraint("CK_HallReviews_RatingStars", "[RatingStars] >= 1 AND [RatingStars] <= 5"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.RatingStars).IsRequired();
        builder.Property(x => x.Comment).HasMaxLength(2000);
        builder.Property(x => x.CreatedUtc).IsRequired();
        builder.HasOne(x => x.Hall)
            .WithMany(x => x.Reviews)
            .HasForeignKey(x => x.HallId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ScheduledSession)
            .WithMany(x => x.HallReviews)
            .HasForeignKey(x => x.ScheduledSessionId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.HallId, x.UserId });
        builder.HasIndex(x => new { x.UserId, x.ScheduledSessionId })
            .IsUnique()
            .HasFilter("[ScheduledSessionId] IS NOT NULL");
    }
}

