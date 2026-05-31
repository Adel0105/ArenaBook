using ArenaBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaBook.Infrastructure.Persistence.Configurations;

public sealed class HallPhotoConfiguration : IEntityTypeConfiguration<HallPhoto>
{
    public void Configure(EntityTypeBuilder<HallPhoto> builder)
    {
        builder.ToTable("HallPhotos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ImageUrl).HasMaxLength(2048).IsRequired();
        builder.HasOne(x => x.Hall)
            .WithMany(x => x.Photos)
            .HasForeignKey(x => x.HallId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.HallId, x.SortOrder });
    }
}

