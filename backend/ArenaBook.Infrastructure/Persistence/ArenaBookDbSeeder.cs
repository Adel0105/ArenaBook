using ArenaBook.Application.Abstractions;
using ArenaBook.Domain.Entities;
using ArenaBook.Domain.Security;
using ArenaBook.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ArenaBook.Infrastructure.Persistence;

public static class ArenaBookDbSeeder
{
    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration,
        bool isDevelopment,
        CancellationToken cancellationToken = default)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(ArenaBookDbSeeder));
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<ArenaBookDbContext>();

        await SeedRolesAsync(roleManager, cancellationToken);

        var usersPassword = ResolveSeedPassword(configuration, isDevelopment, logger);
        if (!string.IsNullOrEmpty(usersPassword))
            await SeedAdminUserAsync(userManager, configuration, usersPassword, cancellationToken);
        else
            logger.LogWarning("Lozinka za seed korisnike nije postavljena. Preskačem kreiranje admina (postavite Seed__UsersPassword).");

        await EnsurePlatformSettingsAsync(db, cancellationToken);

        var runDemo = configuration.GetValue("Seed:RunDemoDataOnStartup", isDevelopment);
        if (runDemo)
        {
            try
            {
                var demoSeed = services.GetRequiredService<IDemoDataSeedService>();
                await demoSeed.SeedAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Demo seed nije uspio u potpunosti; API se i dalje pokreće.");
            }
        }
    }

    private static string? ResolveSeedPassword(
        IConfiguration configuration,
        bool isDevelopment,
        ILogger logger)
    {
        var pwd = configuration["Seed:UsersPassword"];
        if (!string.IsNullOrWhiteSpace(pwd))
            return pwd;

        if (isDevelopment)
        {
            logger.LogWarning(
                "Seed:UsersPassword nije postavljen — u Development modu koristi se podrazumijevana dev lozinka (postavite Seed__UsersPassword za eksplicitnu vrijednost).");
            return "Dev_ArenaBook2026!";
        }

        return null;
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, CancellationToken ct)
    {
        foreach (var name in new[]
                 {
                     ApplicationRoles.Administrator,
                     ApplicationRoles.Organizer,
                     ApplicationRoles.Member,
                 })
        {
            if (await roleManager.RoleExistsAsync(name))
                continue;

            var result = await roleManager.CreateAsync(new IdentityRole(name));
            if (!result.Succeeded)
                throw new InvalidOperationException($"Neuspješno kreiranje uloge {name}: " +
                    string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    private static async Task SeedAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        string password,
        CancellationToken ct)
    {
        var adminEmail = configuration["Seed:AdminEmail"] ?? "admin@arena.local";
        await EnsureUserAsync(
            userManager, adminEmail, "Admin", "Korisnik", 1, ApplicationRoles.Administrator, password, null, ct);
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string firstName,
        string lastName,
        int? cityId,
        string roleName,
        string password,
        DateOnly? dateOfBirth,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (!await userManager.IsInRoleAsync(existing, roleName))
                await userManager.AddToRoleAsync(existing, roleName);

            if (dateOfBirth.HasValue && existing.DateOfBirth is null)
            {
                existing.DateOfBirth = dateOfBirth;
                await userManager.UpdateAsync(existing);
            }

            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            CityId = cityId,
            DateOfBirth = dateOfBirth,
            CreatedUtc = DateTime.UtcNow,
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Seed korisnika {email} nije uspio: " +
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(user, roleName);
    }

    private static async Task EnsurePlatformSettingsAsync(ArenaBookDbContext db, CancellationToken ct)
    {
        await UpsertPlatformSettingAsync(db, "Platform.Session.MaxParticipantsPerSession", "22", ct);
        await UpsertPlatformSettingAsync(db, "Platform.Session.MinSessionPriceCoins", "50", ct);
        await UpsertPlatformSettingAsync(db, "Platform.Coins.CoinsPerBam", "10", ct);
        await UpsertPlatformSettingAsync(db, "Platform.Coins.MinPurchaseCoins", "10", ct);
        await UpsertPlatformSettingAsync(db, "Platform.Coins.MaxPurchaseCoins", "100000", ct);
        await UpsertPlatformSettingAsync(
            db,
            "CoinPurchase.DisplayHint",
            "1 KM ≈ 10 Arena novčića (seed vrijednost za testiranje).",
            ct);
        await db.SaveChangesAsync(ct);
    }

    private static async Task UpsertPlatformSettingAsync(
        ArenaBookDbContext db,
        string key,
        string value,
        CancellationToken ct)
    {
        if (await db.PlatformSettingEntries.AnyAsync(x => x.SettingKey == key, ct))
            return;

        db.PlatformSettingEntries.Add(new PlatformSettingEntry
        {
            SettingKey = key,
            SettingValue = value,
            UpdatedUtc = DateTime.UtcNow,
        });
    }

}

