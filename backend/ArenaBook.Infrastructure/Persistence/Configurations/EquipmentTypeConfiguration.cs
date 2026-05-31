using ArenaBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaBook.Infrastructure.Persistence.Configurations;

public sealed class EquipmentTypeConfiguration : IEntityTypeConfiguration<EquipmentType>
{
    public void Configure(EntityTypeBuilder<EquipmentType> builder)
    {
        builder.ToTable("EquipmentTypes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.HasData(
            new EquipmentType { Id = 1, Name = "Vještačka trava" },
            new EquipmentType { Id = 2, Name = "LED rasvjeta" },
            new EquipmentType { Id = 3, Name = "Gol mreže i konstrukcija" });
    }
}

