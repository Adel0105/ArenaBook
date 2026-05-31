using ArenaBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaBook.Infrastructure.Persistence.Configurations;

public sealed class PlatformSettingEntryConfiguration : IEntityTypeConfiguration<PlatformSettingEntry>
{
    public void Configure(EntityTypeBuilder<PlatformSettingEntry> builder)
    {
        builder.ToTable("PlatformSettingEntries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SettingKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.SettingValue).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.UpdatedUtc).IsRequired();
        builder.HasIndex(x => x.SettingKey).IsUnique();
    }
}

