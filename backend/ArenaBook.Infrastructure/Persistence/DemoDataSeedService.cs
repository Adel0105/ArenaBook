using ArenaBook.Application.Abstractions;
using ArenaBook.Application.Sessions;
using ArenaBook.Domain;
using ArenaBook.Domain.Entities;
using ArenaBook.Domain.Security;
using ArenaBook.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArenaBook.Infrastructure.Persistence;

public sealed class DemoDataSeedService : IDemoDataSeedService
{
    private const int HallsPerCity = 10;
    private const int TargetDemoPlayers = 20;
    private const int DemoHallCapacityBase = 20;
    private const int DemoHallCapacityStep = 6;
    private const decimal DemoHallPriceBaseCoins = 80m;
    private const decimal DemoHallPriceStepCoins = 15m;
    private const decimal DemoWalletBalanceBaseCoins = 300m;
    private const decimal DemoWalletBalanceStepCoins = 25m;
    private const decimal DemoMissingWalletBalanceCoins = 500m;

    private static readonly string[] CityNames =
    [
        "Sarajevo",
        "Banja Luka",
        "Tuzla",
        "Zenica",
        "Mostar",
        "Bihać",
        "Brčko",
        "Trebinje",
    ];

    private static readonly (string First, string Last, string Role)[] DemoPlayers =
    [
        ("Amir", "Hadžić", ApplicationRoles.Member),
        ("Tarik", "Selimović", ApplicationRoles.Organizer),
        ("Dino", "Bašić", ApplicationRoles.Member),
        ("Haris", "Delić", ApplicationRoles.Member),
        ("Nermin", "Jusić", ApplicationRoles.Organizer),
        ("Edin", "Karić", ApplicationRoles.Member),
        ("Kenan", "Osmanović", ApplicationRoles.Member),
        ("Adnan", "Tomić", ApplicationRoles.Organizer),
        ("Faruk", "Zukić", ApplicationRoles.Member),
        ("Mirza", "Džafić", ApplicationRoles.Member),
        ("Emir", "Bećirović", ApplicationRoles.Organizer),
        ("Armin", "Hodžić", ApplicationRoles.Member),
        ("Samed", "Kovač", ApplicationRoles.Member),
        ("Vedad", "Osman", ApplicationRoles.Organizer),
        ("Alen", "Mušić", ApplicationRoles.Member),
        ("Nedim", "Čengić", ApplicationRoles.Member),
        ("Senad", "Jahić", ApplicationRoles.Organizer),
        ("Elvis", "Bajramović", ApplicationRoles.Member),
        ("Damir", "Softić", ApplicationRoles.Member),
        ("Ibrahim", "Nuhić", ApplicationRoles.Organizer),
    ];

    private readonly ArenaBookDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISessionOrganizerRoleService _organizerRoleService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DemoDataSeedService> _logger;

    public DemoDataSeedService(
        ArenaBookDbContext db,
        UserManager<ApplicationUser> userManager,
        ISessionOrganizerRoleService organizerRoleService,
        IConfiguration configuration,
        ILogger<DemoDataSeedService> logger)
    {
        _db = db;
        _userManager = userManager;
        _organizerRoleService = organizerRoleService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<DemoDataSeedResult> SeedAsync(CancellationToken cancellationToken = default)
    {
        var password = ResolveSeedPassword();
        if (password is null)
        {
            _logger.LogWarning("Demo seed preskočen: Seed:UsersPassword nije postavljen.");
            return new DemoDataSeedResult();
        }

        var countryId = await EnsureBosniaCountryAsync(cancellationToken);
        var cityMap = await EnsureCitiesAsync(countryId, cancellationToken);
        var hallsCreated = await EnsureHallsAsync(cityMap, cancellationToken);
        var hallPhotosUpdated = await EnsureHallPhotosAsync(cancellationToken);
        var playersCreated = await EnsureDemoPlayersAsync(password, cityMap, cancellationToken);
        await SeedWalletsForUsersWithoutWalletAsync(cancellationToken);
        var sessionsCreated = await EnsureDemoSessionsAsync(cancellationToken);
        var paymentsCreated = await EnsureDemoPaymentsAsync(cancellationToken);
        var engagementSeeded = await EnsureDemoHallEngagementAsync(cancellationToken);

        _logger.LogInformation(
            "Demo seed: gradovi={Cities}, nove dvorane={Halls}, ažurirane fotografije dvorana={HallPhotos}, novi igrači={Players}, novi termini={Sessions}, nova plaćanja={Payments}, recenzije/lajkovi={Engagement}",
            cityMap.Count,
            hallsCreated,
            hallPhotosUpdated,
            playersCreated,
            sessionsCreated,
            paymentsCreated,
            engagementSeeded);

        return new DemoDataSeedResult
        {
            CitiesEnsured = cityMap.Count,
            HallsCreated = hallsCreated,
            PlayersCreated = playersCreated,
            SessionsCreated = sessionsCreated,
            PaymentsCreated = paymentsCreated,
        };
    }

    private string? ResolveSeedPassword()
    {
        var pwd = _configuration["Seed:UsersPassword"];
        if (!string.IsNullOrWhiteSpace(pwd))
            return pwd;

        return "Dev_ArenaBook2026!";
    }

    private async Task<int> EnsureBosniaCountryAsync(CancellationToken ct)
    {
        const string countryName = "Bosna i Hercegovina";
        var existing = await _db.Countries.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name == countryName, ct);
        if (existing is not null)
            return existing.Id;

        var extra = await _db.Countries.Where(c => c.Id != 1).ToListAsync(ct);
        if (extra.Count > 0)
            _db.Countries.RemoveRange(extra);

        var country = await _db.Countries.FirstOrDefaultAsync(c => c.Id == 1, ct);
        if (country is null)
        {
            _db.Countries.Add(new Country { Id = 1, Name = countryName });
            await _db.SaveChangesAsync(ct);
            return 1;
        }

        if (country.Name != countryName)
        {
            country.Name = countryName;
            await _db.SaveChangesAsync(ct);
        }

        return country.Id;
    }

    private async Task<Dictionary<string, int>> EnsureCitiesAsync(int countryId, CancellationToken ct)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in CityNames)
        {
            var city = await _db.Cities.FirstOrDefaultAsync(c => c.Name == name, ct);
            if (city is null)
            {
                city = new City { CountryId = countryId, Name = name };
                _db.Cities.Add(city);
                await _db.SaveChangesAsync(ct);
            }
            else if (city.CountryId != countryId)
            {
                city.CountryId = countryId;
                await _db.SaveChangesAsync(ct);
            }

            map[name] = city.Id;
        }

        return map;
    }

    private async Task<int> EnsureHallsAsync(IReadOnlyDictionary<string, int> cityMap, CancellationToken ct)
    {
        var created = 0;
        var hallPrefixes = new[]
        {
            "Arena",
            "Sportski centar",
            "Fudbalski kompleks",
            "Gradska dvorana",
            "Nogometni park",
            "FC",
            "Urban futsal",
            "Elite hall",
            "Dinamo",
            "Union",
        };

        var coords = new Dictionary<string, (decimal Lat, decimal Lng)>(StringComparer.OrdinalIgnoreCase)
        {
            ["Sarajevo"] = (43.8563m, 18.4131m),
            ["Banja Luka"] = (44.7722m, 17.1910m),
            ["Tuzla"] = (44.5384m, 18.6762m),
            ["Zenica"] = (44.2014m, 17.9074m),
            ["Mostar"] = (43.3438m, 17.8078m),
            ["Bihać"] = (44.8167m, 15.8708m),
            ["Brčko"] = (44.8694m, 18.8103m),
            ["Trebinje"] = (42.7125m, 18.3439m),
        };

        foreach (var (cityName, cityId) in cityMap)
        {
            var existingCount = await _db.Halls.CountAsync(h => h.CityId == cityId, ct);
            if (existingCount >= HallsPerCity)
                continue;

            var (baseLat, baseLng) = coords.TryGetValue(cityName, out var c)
                ? c
                : (43.9159m, 17.6791m);

            for (var i = existingCount; i < HallsPerCity; i++)
            {
                var prefix = hallPrefixes[i % hallPrefixes.Length];
                var hall = new Hall
                {
                    Name = $"{prefix} {cityName} {(i + 1)}",
                    CityId = cityId,
                    StreetAddress = $"Ulica {i + 1}, {cityName}",
                    Latitude = baseLat + (i * 0.001m),
                    Longitude = baseLng + (i * 0.001m),
                    CapacityPeople = DemoHallCapacityBase + (i % 5) * DemoHallCapacityStep,
                    PricePerHourCoins = DemoHallPriceBaseCoins + (i % 7) * DemoHallPriceStepCoins,
                    ContactPhone = $"+387 6{(i % 9)} {100 + i:000} {200 + i:000}",
                    IsActive = i % 9 != 8,
                    CreatedUtc = DateTime.UtcNow.AddDays(-(i + 3)),
                };
                var hallIndex = await _db.Halls.CountAsync(ct);
                _db.Halls.Add(hall);
                await _db.SaveChangesAsync(ct);

                _db.HallPhotos.Add(new HallPhoto
                {
                    HallId = hall.Id,
                    SortOrder = 1,
                    ImageUrl = DemoHallPhotoCatalog.GetUrlForHallIndex(hallIndex),
                });
                var secondEquipmentTypeId = (i % 3) + 1;
                if (secondEquipmentTypeId == 1)
                    secondEquipmentTypeId = 2;
                _db.HallEquipments.AddRange(
                    new HallEquipment { HallId = hall.Id, EquipmentTypeId = 1, Quantity = 1 },
                    new HallEquipment { HallId = hall.Id, EquipmentTypeId = secondEquipmentTypeId, Quantity = 1 + (i % 2) });
                await _db.SaveChangesAsync(ct);
                created++;
            }
        }

        return created;
    }

    private async Task<int> EnsureHallPhotosAsync(CancellationToken ct)
    {
        var halls = await _db.Halls.OrderBy(h => h.Id).ToListAsync(ct);
        var updated = 0;

        for (var i = 0; i < halls.Count; i++)
        {
            var hall = halls[i];
            var url = DemoHallPhotoCatalog.GetUrlForHallIndex(i);
            var photo = await _db.HallPhotos
                .Where(p => p.HallId == hall.Id)
                .OrderBy(p => p.SortOrder)
                .FirstOrDefaultAsync(ct);

            if (photo is null)
            {
                _db.HallPhotos.Add(new HallPhoto
                {
                    HallId = hall.Id,
                    SortOrder = 1,
                    ImageUrl = url,
                });
                updated++;
                continue;
            }

            if (!NeedsDemoPhotoReplacement(photo.ImageUrl))
                continue;

            photo.ImageUrl = url;
            updated++;
        }

        if (updated > 0)
            await _db.SaveChangesAsync(ct);

        return updated;
    }

    private static bool NeedsDemoPhotoReplacement(string imageUrl) =>
        imageUrl.Contains("picsum.photos", StringComparison.OrdinalIgnoreCase)
        || imageUrl.Contains("loremflickr.com", StringComparison.OrdinalIgnoreCase)
        || imageUrl.Contains("placehold.co", StringComparison.OrdinalIgnoreCase);

    private async Task<int> EnsureDemoPlayersAsync(
        string password,
        IReadOnlyDictionary<string, int> cityMap,
        CancellationToken ct)
    {
        var adminEmail = (_configuration["Seed:AdminEmail"] ?? "admin@arena.local").Trim().ToLowerInvariant();
        var created = 0;
        var cityIds = cityMap.Values.ToList();
        if (cityIds.Count == 0)
            return 0;

        for (var i = 0; i < DemoPlayers.Length; i++)
        {
            var (first, last, role) = DemoPlayers[i];
            var localPart = $"{ToAsciiSlug(first)}.{ToAsciiSlug(last)}";
            var email = $"{localPart}@arena.local";

            if (email.Equals(adminEmail, StringComparison.OrdinalIgnoreCase))
                continue;

            var existing = await _userManager.FindByEmailAsync(email);
            if (existing is not null)
            {
                if (!await _userManager.IsInRoleAsync(existing, role))
                    await _userManager.AddToRoleAsync(existing, role);
                continue;
            }

            var cityId = cityIds[i % cityIds.Count];
            var dob = new DateOnly(1990 + (i % 12), (i % 12) + 1, Math.Min(10 + i, 28));
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = first,
                LastName = last,
                CityId = cityId,
                DateOfBirth = dob,
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                _logger.LogWarning(
                    "Demo korisnik {Email} nije kreiran: {Errors}",
                    email,
                    string.Join("; ", result.Errors.Select(e => e.Description)));
                continue;
            }

            await _userManager.AddToRoleAsync(user, role);
            if (role == ApplicationRoles.Organizer)
                await _organizerRoleService.EnsureOrganizerRoleForUserAsync(user.Id, ct);

            var walletUtc = DateTime.UtcNow.AddMonths(-(i % 6)).AddDays(-i);
            var wallet = new UserCoinWallet
            {
                UserId = user.Id,
                BalanceCoins = DemoWalletBalanceBaseCoins + (i * DemoWalletBalanceStepCoins),
                UpdatedUtc = walletUtc,
            };
            wallet.LedgerEntries.Add(new CoinLedgerEntry
            {
                AmountCoins = wallet.BalanceCoins,
                BalanceAfter = wallet.BalanceCoins,
                ReasonCode = CoinLedgerReasonCodes.SeedInitial,
                CreatedUtc = walletUtc,
            });
            _db.UserCoinWallets.Add(wallet);
            await _db.SaveChangesAsync(ct);
            created++;
        }

        return created;
    }

    private async Task SeedWalletsForUsersWithoutWalletAsync(CancellationToken ct)
    {
        var withWallet = (await _db.UserCoinWallets.Select(w => w.UserId).ToListAsync(ct)).ToHashSet();
        var users = await _db.Users.Select(u => new { u.Id }).ToListAsync(ct);

        foreach (var user in users)
        {
            if (withWallet.Contains(user.Id))
                continue;

            var initial = DemoMissingWalletBalanceCoins;
            var now = DateTime.UtcNow;
            var wallet = new UserCoinWallet
            {
                UserId = user.Id,
                BalanceCoins = initial,
                UpdatedUtc = now,
            };
            wallet.LedgerEntries.Add(new CoinLedgerEntry
            {
                AmountCoins = initial,
                BalanceAfter = initial,
                ReasonCode = CoinLedgerReasonCodes.SeedInitial,
                CreatedUtc = now,
            });
            _db.UserCoinWallets.Add(wallet);
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task<int> EnsureDemoSessionsAsync(CancellationToken ct)
    {
        const int targetSessions = 40;
        var current = await _db.ScheduledSessions.CountAsync(ct);
        if (current >= targetSessions)
            return 0;

        var halls = await _db.Halls.Where(h => h.IsActive).OrderBy(h => h.Id).ToListAsync(ct);
        var players = await _userManager.GetUsersInRoleAsync(ApplicationRoles.Member);
        var organizers = await _userManager.GetUsersInRoleAsync(ApplicationRoles.Organizer);
        var allOrganizers = players.Concat(organizers).DistinctBy(u => u.Id).ToList();
        if (halls.Count == 0 || allOrganizers.Count == 0)
            return 0;

        var created = 0;
        var rng = new Random(210169);
        var toCreate = targetSessions - current;

        for (var n = 0; n < toCreate; n++)
        {
            var hall = halls[rng.Next(halls.Count)];
            var organizer = allOrganizers[rng.Next(allOrganizers.Count)];
            var daysOffset = rng.Next(-90, 21);
            var hour = 16 + rng.Next(0, 5);
            var start = DateTime.UtcNow.Date.AddDays(daysOffset).AddHours(hour);
            var statusRoll = rng.Next(100);
            var statusId = statusRoll switch
            {
                < 15 => 1,
                < 25 => 3,
                < 40 => 4,
                _ => 2,
            };
            var kindId = rng.Next(100) < 80 ? 1 : 2;
            var end = start.AddHours(2);
            var totalPrice = SessionPricing.ComputeTotalPrice(hall.PricePerHourCoins, start, end);

            var session = new ScheduledSession
            {
                HallId = hall.Id,
                OrganizerUserId = organizer.Id,
                SessionKindId = kindId,
                SessionLifecycleStatusId = statusId,
                StartUtc = start,
                EndUtc = end,
                PriceTotalCoins = totalPrice,
                PricePerParticipantCoins = SessionPricing.ComputeParticipantJoinPrice(totalPrice),
                MaxParticipants = 8 + rng.Next(0, 14),
                MaxAgeYears = kindId == 1 ? 18 + rng.Next(0, 15) : null,
                InviteCode = kindId == 2 ? $"INV-{hall.Id}-{n:D4}" : null,
                CreatedUtc = start.AddDays(-rng.Next(1, 10)),
            };
            _db.ScheduledSessions.Add(session);
            await _db.SaveChangesAsync(ct);

            if (statusId == 4)
            {
                _db.ScheduledSessionParticipants.Add(new ScheduledSessionParticipant
                {
                    ScheduledSessionId = session.Id,
                    UserId = organizer.Id,
                    JoinedUtc = start.AddMinutes(-30),
                    CoinsPaid = 0,
                    IsOrganizer = true,
                });
            }

            created++;
        }

        await _db.SaveChangesAsync(ct);
        return created;
    }

    private async Task<int> EnsureDemoPaymentsAsync(CancellationToken ct)
    {
        const int targetPayments = 35;
        var current = await _db.ExternalPaymentRecords.CountAsync(ct);
        if (current >= targetPayments)
            return 0;

        var memberUsers = await _userManager.GetUsersInRoleAsync(ApplicationRoles.Member);
        var organizerUsers = await _userManager.GetUsersInRoleAsync(ApplicationRoles.Organizer);
        var users = memberUsers.Concat(organizerUsers).DistinctBy(u => u.Id).ToList();
        if (users.Count == 0)
            return 0;

        var created = 0;
        var rng = new Random(210169 + 7);
        var toCreate = targetPayments - current;
        var providers = new[] { "PayPal", "Card" };
        var purposes = new[] { "COIN_PURCHASE", "SESSION_PAYMENT" };
        var statuses = new[] { 1, 2, 2, 2, 3, 4 };

        for (var i = 0; i < toCreate; i++)
        {
            var user = users[rng.Next(users.Count)];
            var purpose = purposes[rng.Next(purposes.Length)];
            var daysAgo = rng.Next(0, 120);
            var createdUtc = DateTime.UtcNow.AddDays(-daysAgo).AddHours(-rng.Next(0, 12));
            _db.ExternalPaymentRecords.Add(new ExternalPaymentRecord
            {
                UserId = user.Id,
                PurposeCode = purpose,
                Provider = providers[rng.Next(providers.Length)],
                AmountMoney = Math.Round((decimal)(5 + rng.NextDouble() * 45), 2),
                Currency = "BAM",
                PaymentProcessingStatusId = statuses[rng.Next(statuses.Length)],
                ExternalReference = $"SEED-{purpose}-{user.Id[..8]}-{createdUtc:yyyyMMdd}-{i}",
                CoinsPurchased = purpose == "COIN_PURCHASE" ? 50 + rng.Next(0, 450) : 0,
                CreatedUtc = createdUtc,
            });
            created++;
        }

        await _db.SaveChangesAsync(ct);
        return created;
    }

    private async Task<int> EnsureDemoHallEngagementAsync(CancellationToken ct)
    {
        const int targetReviews = 120;
        var currentReviews = await _db.HallReviews.CountAsync(ct);
        var created = 0;

        if (currentReviews < targetReviews)
        {
            var halls = await _db.Halls.OrderBy(h => h.Id).Take(40).ToListAsync(ct);
            var users = await _userManager.GetUsersInRoleAsync(ApplicationRoles.Member);
            users = users.Concat(await _userManager.GetUsersInRoleAsync(ApplicationRoles.Organizer))
                .DistinctBy(u => u.Id)
                .ToList();
            if (halls.Count > 0 && users.Count > 0)
            {
                var rng = new Random(424242);
                var comments = new[]
                {
                    "Odličan teren i čistoća.",
                    "Parking je malo usko, ali dvorana je super.",
                    "Svjetla su dobra za večernje termine.",
                    "Oprema je u redu, preporučujem.",
                    "Malo skupo, ali kvalitet odgovara cijeni.",
                    "Organizacija je bila besprijekorna.",
                    "Prostor je dovoljno velik za našu ekipu.",
                };

                var existingPairs = (await _db.HallReviews
                    .Select(r => new { r.HallId, r.UserId })
                    .ToListAsync(ct))
                    .Select(x => (x.HallId, x.UserId))
                    .ToHashSet();

                var toCreate = targetReviews - currentReviews;
                for (var i = 0; i < toCreate; i++)
                {
                    var hall = halls[rng.Next(halls.Count)];
                    var user = users[rng.Next(users.Count)];
                    if (!existingPairs.Add((hall.Id, user.Id)))
                        continue;

                    _db.HallReviews.Add(new HallReview
                    {
                        HallId = hall.Id,
                        UserId = user.Id,
                        RatingStars = (byte)(3 + rng.Next(0, 3)),
                        Comment = comments[rng.Next(comments.Length)],
                        CreatedUtc = DateTime.UtcNow.AddDays(-rng.Next(1, 90)),
                    });
                    created++;
                }

                await _db.SaveChangesAsync(ct);
            }
        }

        var reactionCount = await _db.HallReactions.CountAsync(ct);
        if (reactionCount < 80)
        {
            var halls = await _db.Halls.OrderBy(h => h.Id).Take(40).ToListAsync(ct);
            var users = await _userManager.GetUsersInRoleAsync(ApplicationRoles.Member);
            users = users.Concat(await _userManager.GetUsersInRoleAsync(ApplicationRoles.Organizer))
                .DistinctBy(u => u.Id)
                .ToList();
            if (halls.Count > 0 && users.Count > 0)
            {
                var rng = new Random(424243);
                var existingReactions = (await _db.HallReactions
                    .Select(r => new { r.HallId, r.UserId })
                    .ToListAsync(ct))
                    .Select(x => (x.HallId, x.UserId))
                    .ToHashSet();

                foreach (var hall in halls)
                {
                    foreach (var user in users.OrderBy(_ => rng.Next()).Take(3))
                    {
                        if (!existingReactions.Add((hall.Id, user.Id)))
                            continue;

                        var now = DateTime.UtcNow.AddDays(-rng.Next(1, 30));
                        _db.HallReactions.Add(new HallReaction
                        {
                            HallId = hall.Id,
                            UserId = user.Id,
                            IsLike = rng.Next(100) < 82,
                            CreatedUtc = now,
                            UpdatedUtc = now,
                        });
                        created++;
                    }
                }

                await _db.SaveChangesAsync(ct);
            }
        }

        return created;
    }

    private static string ToAsciiSlug(string value)
    {
        var map = new Dictionary<char, string>
        {
            ['č'] = "c", ['ć'] = "c", ['ž'] = "z", ['š'] = "s", ['đ'] = "d",
            ['Č'] = "c", ['Ć'] = "c", ['Ž'] = "z", ['Š'] = "s", ['Đ'] = "d",
        };
        var chars = value.Trim().ToLowerInvariant()
            .Select(ch => map.TryGetValue(ch, out var r) ? r : char.IsLetterOrDigit(ch) ? ch.ToString() : "")
            .ToArray();
        return string.Concat(chars);
    }
}

