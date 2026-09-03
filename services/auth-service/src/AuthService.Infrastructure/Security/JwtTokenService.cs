using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthService.Application.Common.Interfaces;
using AuthService.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Infrastructure.Security;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options) => _options = options.Value;

    public AccessTokenResult GenerateAccessToken(User user, IReadOnlyCollection<string> roles)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_options.AccessTokenLifetimeMinutes);

        var claims = new List<global::System.Security.Claims.Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, ((DateTimeOffset)now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("first_name", user.FirstName),
            new("last_name", user.LastName),
            // Every authenticated principal is also a potential customer; downstream
            // services (booking, payment, ticketing) key ownership off customer_id.
            new("customer_id", user.Id.ToString()),
            // Single-tenant until M10 (SaaS foundation) — the platform default
            // tenant. The gateway's TenantHeaderHygieneMiddleware re-injects
            // X-Tenant-Id from this claim; downstream services read it for
            // isolation filters. See docs/PRODUCTION-MILESTONES.md M10 / M1.
            new("tenant_id", _options.DefaultTenantId)
        };
        if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
            claims.Add(new global::System.Security.Claims.Claim("phone_number", user.PhoneNumber));
        claims.AddRange(roles.Select(role => new global::System.Security.Claims.Claim(ClaimTypes.Role, role)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        var handler = new JwtSecurityTokenHandler();
        return new AccessTokenResult(handler.WriteToken(token), expires);
    }

    public RefreshTokenResult GenerateRefreshToken()
    {
        var rawBytes = RandomNumberGenerator.GetBytes(64);
        var raw = Convert.ToBase64String(rawBytes);
        return new RefreshTokenResult(raw, HashRefreshToken(raw), TimeSpan.FromDays(_options.RefreshTokenLifetimeDays));
    }

    public string HashRefreshToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }

    public global::System.Security.Claims.ClaimsPrincipal? ValidateAccessToken(string accessToken)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _options.Issuer,
                ValidateAudience = true,
                ValidAudience = _options.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            var principal = handler.ValidateToken(accessToken, parameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }
}
