using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ArenaBook.Application.Contracts.Auth;
using ArenaBook.Application.Options;
using ArenaBook.Infrastructure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ArenaBook.Infrastructure.Authentication;

public sealed class JwtTokenFactory
{
    private readonly JwtSettings _settings;

    public JwtTokenFactory(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
    }

    public AuthTokensResponse CreateTokens(ApplicationUser user, IList<string> roles)
    {
        var expires = DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jwtId = Guid.NewGuid().ToString("N");
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, jwtId),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return new AuthTokensResponse
        {
            AccessToken = jwt,
            TokenType = "Bearer",
            ExpiresInSeconds = (int)TimeSpan.FromMinutes(_settings.AccessTokenMinutes).TotalSeconds,
            ExpiresAtUtc = new DateTimeOffset(expires, TimeSpan.Zero),
        };
    }
}

