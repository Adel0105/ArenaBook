using ArenaBook.Domain.Entities;
using ArenaBook.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaBook.Infrastructure.Persistence.Configurations;

public sealed class HallReactionConfiguration : IEntityTypeConfiguration<HallReaction>
{
    public void Configure(EntityTypeBuilder<HallReaction> builder)
    {
        builder.ToTable("HallReactions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.IsLike).IsRequired();
        builder.Property(x => x.CreatedUtc).IsRequired();
        builder.Property(x => x.UpdatedUtc).IsRequired();
        builder.HasOne(x => x.Hall)
            .WithMany(x => x.Reactions)
            .HasForeignKey(x => x.HallId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.HallId, x.UserId }).IsUnique();
    }
}

