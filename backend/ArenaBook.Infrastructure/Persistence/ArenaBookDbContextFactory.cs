using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ArenaBook.Infrastructure.Persistence;

public sealed class ArenaBookDbContextFactory : IDesignTimeDbContextFactory<ArenaBookDbContext>
{
    public ArenaBookDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ArenaBookDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=localhost,1433;Database=ArenaBook;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;MultipleActiveResultSets=true";
        optionsBuilder.UseSqlServer(connectionString);
        return new ArenaBookDbContext(optionsBuilder.Options);
    }
}

