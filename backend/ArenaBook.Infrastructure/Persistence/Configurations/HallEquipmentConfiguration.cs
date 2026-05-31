using ArenaBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaBook.Infrastructure.Persistence.Configurations;

public sealed class HallEquipmentConfiguration : IEntityTypeConfiguration<HallEquipment>
{
    public void Configure(EntityTypeBuilder<HallEquipment> builder)
    {
        builder.ToTable("HallEquipments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Quantity).IsRequired();
        builder.HasOne(x => x.Hall)
            .WithMany(x => x.EquipmentLinks)
            .HasForeignKey(x => x.HallId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.EquipmentType)
            .WithMany(x => x.HallEquipments)
            .HasForeignKey(x => x.EquipmentTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.HallId, x.EquipmentTypeId }).IsUnique();
    }
}

