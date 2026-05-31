using ArenaBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaBook.Infrastructure.Persistence.Configurations;

public sealed class RevokedAccessTokenConfiguration : IEntityTypeConfiguration<RevokedAccessToken>
{
    public void Configure(EntityTypeBuilder<RevokedAccessToken> builder)
    {
        builder.ToTable("RevokedAccessTokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.JwtId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ExpiresUtc).IsRequired();
        builder.Property(x => x.RevokedUtc).IsRequired();
        builder.HasIndex(x => x.JwtId).IsUnique();
        builder.HasIndex(x => x.ExpiresUtc);
    }
}
