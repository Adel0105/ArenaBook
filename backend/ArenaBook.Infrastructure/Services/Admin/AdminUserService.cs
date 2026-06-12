using ArenaBook.Application.Abstractions.Admin;
using ArenaBook.Application.Common.Exceptions;
using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Admin;
using ArenaBook.Domain.Entities;
using ArenaBook.Domain.Security;
using ArenaBook.Infrastructure.Identity;
using ArenaBook.Infrastructure.Persistence;
using ArenaBook.Infrastructure.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ArenaBook.Infrastructure.Services.Admin;

public sealed class AdminUserService : IAdminUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ArenaBookDbContext _db;
    private readonly IValidator<CreateAdminUserRequest> _createValidator;
    private readonly IValidator<UpdateAdminUserRequest> _updateValidator;

    public AdminUserService(
        UserManager<ApplicationUser> userManager,
        ArenaBookDbContext db,
        IValidator<CreateAdminUserRequest> createValidator,
        IValidator<UpdateAdminUserRequest> updateValidator)
    {
        _userManager = userManager;
        _db = db;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedListResponse<AdminUserListItemResponse>> GetPagedAsync(
        PageRequest page,
        AdminUserListQuery query,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = page.GetNormalizedPage();
        var normalizedPageSize = page.GetNormalizedPageSize();
        var skip = (normalizedPage - 1) * normalizedPageSize;

        var users = _userManager.Users.AsNoTracking();

        var baseQ =
            from u in users
            join c in _db.Cities.AsNoTracking() on u.CityId equals c.Id into cg
            from c in cg.DefaultIfEmpty()
            select new
            {
                User = u,
                CityName = c != null ? c.Name : null,
            };

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = query.Q.Trim();
            baseQ = baseQ.Where(x =>
                x.User.Email!.Contains(term)
                || x.User.FirstName.Contains(term)
                || x.User.LastName.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(query.Email))
        {
            var email = query.Email.Trim();
            baseQ = baseQ.Where(x => x.User.Email == email);
        }

        if (query.RegisteredFromUtc.HasValue)
            baseQ = baseQ.Where(x => x.User.CreatedUtc >= query.RegisteredFromUtc.Value);

        if (query.RegisteredToUtc.HasValue)
            baseQ = baseQ.Where(x => x.User.CreatedUtc <= query.RegisteredToUtc.Value);

        if (query.IsLockedOut == true)
            baseQ = baseQ.Where(x => x.User.LockoutEnd != null && x.User.LockoutEnd > DateTimeOffset.UtcNow);
        else if (query.IsLockedOut == false)
            baseQ = baseQ.Where(x => x.User.LockoutEnd == null || x.User.LockoutEnd <= DateTimeOffset.UtcNow);

        var total = await baseQ.CountAsync(cancellationToken);
        var rows = await baseQ
            .OrderBy(x => x.User.LastName)
            .ThenBy(x => x.User.FirstName)
            .Skip(skip)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        var items = new List<AdminUserListItemResponse>();
        foreach (var row in rows)
        {
            var roles = await _userManager.GetRolesAsync(row.User);
            items.Add(MapListItem(row.User, roles, row.CityName));
        }

        return new PagedListResponse<AdminUserListItemResponse>
        {
            Items = items,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = total,
        };
    }

    public async Task<AdminUserDetailsResponse> GetByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            throw new NotFoundException("Korisnik nije pronađen.");

        var roles = await _userManager.GetRolesAsync(user);
        var cityName = user.CityId.HasValue
            ? await _db.Cities.AsNoTracking().Where(c => c.Id == user.CityId).Select(c => c.Name).FirstOrDefaultAsync(cancellationToken)
            : null;
        var organized = await _db.ScheduledSessions.CountAsync(s => s.OrganizerUserId == userId, cancellationToken);
        var participated = await _db.ScheduledSessionParticipants.CountAsync(p => p.UserId == userId, cancellationToken);

        return MapDetails(user, roles, cityName, organized, participated);
    }

    public async Task<AdminUserDetailsResponse> CreateAsync(CreateAdminUserRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Validacija nije prošla.",
                validation.ToErrorDictionary());

        if (request.CityId is int cityId && !await _db.Cities.AsNoTracking().AnyAsync(c => c.Id == cityId, cancellationToken))
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "CityId nije važeći.",
                new Dictionary<string, string[]>());

        var user = new ApplicationUser
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            DateOfBirth = request.DateOfBirth,
            CityId = request.CityId,
            EmailConfirmed = true,
            CreatedUtc = DateTime.UtcNow,
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Kreiranje korisnika nije uspjelo.",
                createResult.Errors.GroupBy(e => e.Code).ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()));

        await _userManager.AddToRoleAsync(user, request.RoleName);

        if (!await _db.UserCoinWallets.AnyAsync(w => w.UserId == user.Id, cancellationToken))
        {
            _db.UserCoinWallets.Add(new UserCoinWallet
            {
                UserId = user.Id,
                BalanceCoins = 0,
                UpdatedUtc = DateTime.UtcNow,
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        return await GetByIdAsync(user.Id, cancellationToken);
    }

    public async Task<AdminUserDetailsResponse> UpdateAsync(
        string userId,
        UpdateAdminUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Validacija nije prošla.",
                validation.ToErrorDictionary());

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            throw new NotFoundException("Korisnik nije pronađen.");

        if (request.CityId is int cityId && !await _db.Cities.AsNoTracking().AnyAsync(c => c.Id == cityId, cancellationToken))
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "CityId nije važeći.",
                new Dictionary<string, string[]>());

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.DateOfBirth = request.DateOfBirth;
        user.CityId = request.CityId;
        user.ProfileImageUrl = string.IsNullOrWhiteSpace(request.ProfileImageUrl) ? null : request.ProfileImageUrl.Trim();

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Ažuriranje korisnika nije uspjelo.",
                updateResult.Errors.GroupBy(e => e.Code).ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()));

        return await GetByIdAsync(userId, cancellationToken);
    }

    public async Task SetLockedOutAsync(string userId, bool lockedOut, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            throw new NotFoundException("Korisnik nije pronađen.");

        if (await _userManager.IsInRoleAsync(user, ApplicationRoles.Administrator) && lockedOut)
            throw new ConflictException("Administrator račun se ne može deaktivirati.");

        if (lockedOut)
        {
            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            await _userManager.UpdateSecurityStampAsync(user);
        }
        else
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
        }
    }

    private static AdminUserListItemResponse MapListItem(
        ApplicationUser user,
        IList<string> roles,
        string? cityName)
    {
        var locked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;
        return new AdminUserListItemResponse
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DateOfBirth = user.DateOfBirth,
            CityId = user.CityId,
            CityName = cityName,
            Roles = roles.OrderBy(r => r).ToArray(),
            IsLockedOut = locked,
            LockoutEnd = user.LockoutEnd,
            RegisteredUtc = user.CreatedUtc,
        };
    }

    private static AdminUserDetailsResponse MapDetails(
        ApplicationUser user,
        IList<string> roles,
        string? cityName,
        int organized,
        int participated)
    {
        var locked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;
        return new AdminUserDetailsResponse
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DateOfBirth = user.DateOfBirth,
            CityId = user.CityId,
            CityName = cityName,
            ProfileImageUrl = user.ProfileImageUrl,
            Roles = roles.OrderBy(r => r).ToArray(),
            IsLockedOut = locked,
            LockoutEnd = user.LockoutEnd,
            RegisteredUtc = user.CreatedUtc,
            SessionsOrganizedCount = organized,
            SessionsParticipatedCount = participated,
        };
    }
}

