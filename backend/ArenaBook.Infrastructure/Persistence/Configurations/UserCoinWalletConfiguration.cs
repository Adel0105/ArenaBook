using ArenaBook.Domain.Entities;
using ArenaBook.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArenaBook.Infrastructure.Persistence.Configurations;

public sealed class UserCoinWalletConfiguration : IEntityTypeConfiguration<UserCoinWallet>
{
    public void Configure(EntityTypeBuilder<UserCoinWallet> builder)
    {
        builder.ToTable("UserCoinWallets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.BalanceCoins).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.UpdatedUtc).IsRequired();
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.UserId).IsUnique();
    }
}

