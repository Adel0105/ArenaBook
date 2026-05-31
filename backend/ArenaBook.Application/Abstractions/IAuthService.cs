using ArenaBook.Application.Contracts.Auth;

namespace ArenaBook.Application.Abstractions;

public interface IAuthService
{
    Task<AuthOperationResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<AuthOperationResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<CurrentUserResponse?> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default);

    Task<CurrentUserResponse> UpdateProfileAsync(string userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);

    Task<string?> RequestPasswordResetAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);

    Task LogoutAsync(string? jwtId, DateTime? expiresUtc, CancellationToken cancellationToken = default);
}

