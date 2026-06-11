using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using ArenaBook.Application.Abstractions;
using ArenaBook.Application.Abstractions.Messaging;
using ArenaBook.Application.Auth;
using ArenaBook.Application.Authorization;
using ArenaBook.Application.Contracts.Auth;

namespace ArenaBook.Api.Endpoints;

public static class ArenaBookAuthEndpoints
{
    public static WebApplication MapArenaBookAuthEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Auth");

        auth.MapPost("/register", RegisterAsync).AllowAnonymous();

        auth.MapPost("/login", LoginAsync).AllowAnonymous();

        auth.MapGet("/me", MeAsync).RequireAuthorization();

        auth.MapPut("/me", UpdateProfileAsync).RequireAuthorization();

        auth.MapPost("/change-password", ChangePasswordAsync).RequireAuthorization();

        auth.MapPost("/logout", LogoutAsync).RequireAuthorization();

        auth.MapPost("/forgot-password", ForgotPasswordAsync).AllowAnonymous();

        auth.MapPost("/reset-password", ResetPasswordAsync).AllowAnonymous();

        app.MapGet("/api/admin/ping", () => Results.Ok(new { ok = true, area = "admin" }))
            .RequireAuthorization(AuthPolicies.AdministratorOnly)
            .WithTags("Admin");

        app.MapGet("/api/organizer/ping", () => Results.Ok(new { ok = true, area = "organizer" }))
            .RequireAuthorization(AuthPolicies.OrganizerArea)
            .WithTags("Organizer");

        app.MapGet("/api/app/ping", () => Results.Ok(new { ok = true, area = "player-app" }))
            .RequireAuthorization(AuthPolicies.PlayerApp)
            .WithTags("PlayerApp");

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest body,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(body, cancellationToken);
        return result.Success
            ? Results.Ok(result.Tokens)
            : Results.BadRequest(new { errors = result.Errors });
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest body,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(body, cancellationToken);
        if (result.Success)
            return Results.Ok(result.Tokens);

        if (result.Errors.Count == 1 && result.Errors[0] == AuthMessages.InvalidCredentials)
            return Results.Unauthorized();

        return Results.BadRequest(new { errors = result.Errors });
    }

    private static async Task<IResult> MeAsync(
        ClaimsPrincipal user,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id))
            return Results.Unauthorized();

        var profile = await authService.GetCurrentUserAsync(id, cancellationToken);
        return profile is null ? Results.NotFound() : Results.Ok(profile);
    }

    private static async Task<IResult> UpdateProfileAsync(
        ClaimsPrincipal user,
        IAuthService authService,
        UpdateProfileRequest body,
        CancellationToken cancellationToken)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id))
            return Results.Unauthorized();

        var profile = await authService.UpdateProfileAsync(id, body, cancellationToken);
        return Results.Ok(profile);
    }

    private static async Task<IResult> ChangePasswordAsync(
        ClaimsPrincipal user,
        IAuthService authService,
        ChangePasswordRequest body,
        CancellationToken cancellationToken)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id))
            return Results.Unauthorized();

        var jti = user.FindFirstValue(JwtRegisteredClaimNames.Jti);
        var expRaw = user.FindFirstValue(JwtRegisteredClaimNames.Exp);
        DateTime? expiresUtc = null;
        if (!string.IsNullOrEmpty(expRaw) && long.TryParse(expRaw, out var expSeconds))
            expiresUtc = DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;

        await authService.ChangePasswordAsync(id, body, jti, expiresUtc, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> LogoutAsync(
        ClaimsPrincipal user,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var jti = user.FindFirstValue(JwtRegisteredClaimNames.Jti);
        var expRaw = user.FindFirstValue(JwtRegisteredClaimNames.Exp);
        DateTime? expiresUtc = null;
        if (!string.IsNullOrEmpty(expRaw) && long.TryParse(expRaw, out var expSeconds))
            expiresUtc = DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;

        await authService.LogoutAsync(jti, expiresUtc, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ForgotPasswordAsync(
        IAuthService authService,
        IPasswordResetDispatchService passwordResetDispatch,
        ForgotPasswordRequest body,
        IWebHostEnvironment env,
        CancellationToken cancellationToken)
    {
        if (!env.IsDevelopment() && !passwordResetDispatch.IsAvailable)
        {
            return Results.Problem(
                title: "Reset lozinke nije dostupan",
                detail: "Za slanje e-maila moraju biti konfigurirani RabbitMQ i SMTP (vidi README).",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        PasswordResetResult result;
        try
        {
            result = await authService.RequestPasswordResetAsync(body, env.IsDevelopment(), cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Reset lozinke nije dostupan",
                detail: ex.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        if (result.DevelopmentToken is not null)
        {
            return Results.Ok(new
            {
                message = "Development: SMTP nije aktivan — token je u odgovoru (ne šalje se e-mail).",
                resetToken = result.DevelopmentToken,
            });
        }

        return Results.Ok(new { message = "Ako račun postoji, poslan je e-mail za reset lozinke." });
    }

    private static async Task<IResult> ResetPasswordAsync(
        IAuthService authService,
        ResetPasswordRequest body,
        CancellationToken cancellationToken)
    {
        await authService.ResetPasswordAsync(body, cancellationToken);
        return Results.NoContent();
    }
}

