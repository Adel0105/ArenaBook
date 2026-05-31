using ArenaBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaBook.Infrastructure.Persistence.Configurations;

public sealed class HallConfiguration : IEntityTypeConfiguration<Hall>
{
    public void Configure(EntityTypeBuilder<Hall> builder)
    {
        builder.ToTable("Halls");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.StreetAddress).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Latitude).HasPrecision(9, 6);
        builder.Property(x => x.Longitude).HasPrecision(9, 6);
        builder.Property(x => x.PricePerHourCoins).HasPrecision(18, 2);
        builder.Property(x => x.ContactPhone).HasMaxLength(40).IsRequired();
        builder.Property(x => x.CreatedUtc).IsRequired();
        builder.HasOne(x => x.City)
            .WithMany(x => x.Halls)
            .HasForeignKey(x => x.CityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

