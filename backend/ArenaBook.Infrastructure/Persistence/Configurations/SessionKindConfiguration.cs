using ArenaBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaBook.Infrastructure.Persistence.Configurations;

public sealed class SessionKindConfiguration : IEntityTypeConfiguration<SessionKind>
{
    public void Configure(EntityTypeBuilder<SessionKind> builder)
    {
        builder.ToTable("SessionKinds");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasData(
            new SessionKind { Id = 1, Code = "PUBLIC", DisplayName = "Javni termin" },
            new SessionKind { Id = 2, Code = "INVITE", DisplayName = "Privatni poziv" });
    }
}

