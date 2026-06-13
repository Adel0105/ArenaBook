using System.Security.Claims;
using ArenaBook.Application.Abstractions;
using ArenaBook.Application.Auth;
using ArenaBook.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ArenaBook.Infrastructure.Authentication;

public static class JwtAccessTokenValidator
{
    public static async Task ValidateAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;
        if (principal is null)
            return;

        var jwtId = principal.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti);
        if (!string.IsNullOrEmpty(jwtId))
        {
            var revocation = context.HttpContext.RequestServices
                .GetRequiredService<IJwtTokenRevocationService>();
            if (await revocation.IsRevokedAsync(jwtId, context.HttpContext.RequestAborted))
            {
                context.Fail("Token je opozvan.");
                return;
            }
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return;

        var userManager = context.HttpContext.RequestServices
            .GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            context.Fail("Korisnik nije pronađen.");
            return;
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            context.Fail(AuthMessages.AccountLocked);
            return;
        }

        var stampClaim = principal.FindFirstValue(JwtClaimTypes.SecurityStamp);
        if (!string.Equals(user.SecurityStamp, stampClaim, StringComparison.Ordinal))
            context.Fail("Token više nije važeći.");
    }
}
