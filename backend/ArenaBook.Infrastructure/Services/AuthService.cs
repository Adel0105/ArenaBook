using ArenaBook.Application.Abstractions;
using ArenaBook.Application.Abstractions.Messaging;
using ArenaBook.Application.Auth;
using ArenaBook.Application.Contracts.Auth;
using ArenaBook.Domain.Entities;
using ArenaBook.Domain.Security;
using ArenaBook.Infrastructure.Authentication;
using ArenaBook.Infrastructure.Validation;
using ArenaBook.Infrastructure.Identity;
using ArenaBook.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ArenaBook.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ArenaBookDbContext _db;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<UpdateProfileRequest> _updateProfileValidator;
    private readonly IValidator<ChangePasswordRequest> _changePasswordValidator;
    private readonly IValidator<ForgotPasswordRequest> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordRequest> _resetPasswordValidator;
    private readonly JwtTokenFactory _jwtTokenFactory;
    private readonly IJwtTokenRevocationService _jwtTokenRevocationService;
    private readonly IPasswordResetDispatchService _passwordResetDispatch;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ArenaBookDbContext db,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<UpdateProfileRequest> updateProfileValidator,
        IValidator<ChangePasswordRequest> changePasswordValidator,
        IValidator<ForgotPasswordRequest> forgotPasswordValidator,
        IValidator<ResetPasswordRequest> resetPasswordValidator,
        JwtTokenFactory jwtTokenFactory,
        IJwtTokenRevocationService jwtTokenRevocationService,
        IPasswordResetDispatchService passwordResetDispatch)
    {
        _userManager = userManager;
        _db = db;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _updateProfileValidator = updateProfileValidator;
        _changePasswordValidator = changePasswordValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
        _jwtTokenFactory = jwtTokenFactory;
        _jwtTokenRevocationService = jwtTokenRevocationService;
        _passwordResetDispatch = passwordResetDispatch;
    }

    public async Task<AuthOperationResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return AuthOperationResult.Fail(validation.Errors.Select(e => e.ErrorMessage).ToArray());

        if (request.CityId is int cityId && !await _db.Cities.AsNoTracking().AnyAsync(c => c.Id == cityId, cancellationToken))
            return AuthOperationResult.Fail("CityId nije važeći.");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            DateOfBirth = request.DateOfBirth,
            CityId = request.CityId,
            EmailConfirmed = true,
            CreatedUtc = DateTime.UtcNow,
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return AuthOperationResult.Fail(createResult.Errors.Select(e => e.Description).ToArray());

        await _userManager.AddToRoleAsync(user, ApplicationRoles.Member);

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

        var roles = await _userManager.GetRolesAsync(user);
        var tokens = _jwtTokenFactory.CreateTokens(user, roles);
        return AuthOperationResult.Ok(tokens);
    }

    public async Task<AuthOperationResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return AuthOperationResult.Fail(validation.Errors.Select(e => e.ErrorMessage).ToArray());

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
            return AuthOperationResult.Fail(AuthMessages.InvalidCredentials);

        if (await _userManager.IsLockedOutAsync(user))
            return AuthOperationResult.Fail(AuthMessages.AccountLocked);

        var roles = await _userManager.GetRolesAsync(user);
        var tokens = _jwtTokenFactory.CreateTokens(user, roles);
        return AuthOperationResult.Ok(tokens);
    }

    public async Task<CurrentUserResponse?> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return null;

        var roles = await _userManager.GetRolesAsync(user);
        return new CurrentUserResponse
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DateOfBirth = user.DateOfBirth,
            CityId = user.CityId,
            ProfileImageUrl = user.ProfileImageUrl,
            Roles = roles.OrderBy(r => r).ToArray(),
        };
    }

    public async Task<CurrentUserResponse> UpdateProfileAsync(
        string userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _updateProfileValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Validacija nije prošla.",
                validation.ToErrorDictionary());

        if (request.CityId is int cityId && !await _db.Cities.AsNoTracking().AnyAsync(c => c.Id == cityId, cancellationToken))
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Validacija nije prošla.",
                new Dictionary<string, string[]> { ["cityId"] = ["CityId nije važeći."] });

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            throw new ArenaBook.Application.Common.Exceptions.NotFoundException("Korisnik nije pronađen.");

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.DateOfBirth = request.DateOfBirth;
        user.CityId = request.CityId;
        user.ProfileImageUrl = string.IsNullOrWhiteSpace(request.ProfileImageUrl) ? null : request.ProfileImageUrl.Trim();

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Profil nije ažuriran.",
                updateResult.Errors.GroupBy(e => e.Code).ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()));

        var roles = await _userManager.GetRolesAsync(user);
        return new CurrentUserResponse
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DateOfBirth = user.DateOfBirth,
            CityId = user.CityId,
            ProfileImageUrl = user.ProfileImageUrl,
            Roles = roles.OrderBy(r => r).ToArray(),
        };
    }

    public async Task ChangePasswordAsync(
        string userId,
        ChangePasswordRequest request,
        string? currentJwtId = null,
        DateTime? currentTokenExpiresUtc = null,
        CancellationToken cancellationToken = default)
    {
        var validation = await _changePasswordValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Validacija nije prošla.",
                validation.ToErrorDictionary());

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            throw new ArenaBook.Application.Common.Exceptions.NotFoundException("Korisnik nije pronađen.");

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Lozinka nije promijenjena.",
                result.Errors.GroupBy(e => e.Code).ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()));

        await InvalidateUserAccessTokensAsync(user, currentJwtId, currentTokenExpiresUtc, cancellationToken);
    }

    public async Task<PasswordResetResult> RequestPasswordResetAsync(
        ForgotPasswordRequest request,
        bool exposeDevelopmentTokenFallback,
        CancellationToken cancellationToken = default)
    {
        var validation = await _forgotPasswordValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Validacija nije prošla.",
                validation.ToErrorDictionary());

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return new PasswordResetResult();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        if (_passwordResetDispatch.IsAvailable)
        {
            await _passwordResetDispatch.DispatchAsync(user.Email!, token, cancellationToken);
            return new PasswordResetResult { EmailDispatched = true };
        }

        if (exposeDevelopmentTokenFallback)
            return new PasswordResetResult { DevelopmentToken = token };

        throw new InvalidOperationException(
            "Slanje e-maila za reset lozinke nije konfigurirano (RabbitMQ + SMTP).");
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _resetPasswordValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Validacija nije prošla.",
                validation.ToErrorDictionary());

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Reset nije uspio.",
                new Dictionary<string, string[]> { ["email"] = ["Neispravan zahtjev."] });

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Reset nije uspio.",
                result.Errors.GroupBy(e => e.Code).ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()));

        await InvalidateUserAccessTokensAsync(user, currentJwtId: null, currentTokenExpiresUtc: null, cancellationToken);
    }

    public Task LogoutAsync(string? jwtId, DateTime? expiresUtc, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jwtId) || expiresUtc is null)
            return Task.CompletedTask;

        return _jwtTokenRevocationService.RevokeAsync(jwtId, expiresUtc.Value, cancellationToken);
    }

    private async Task InvalidateUserAccessTokensAsync(
        ApplicationUser user,
        string? currentJwtId,
        DateTime? currentTokenExpiresUtc,
        CancellationToken cancellationToken)
    {
        await _userManager.UpdateSecurityStampAsync(user);
        await LogoutAsync(currentJwtId, currentTokenExpiresUtc, cancellationToken);
    }
}

