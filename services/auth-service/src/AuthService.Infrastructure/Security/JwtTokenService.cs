using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthService.Application.Common.Interfaces;
using AuthService.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Infrastructure.Security;

/// <summary>
/// Issues short-lived (15 min default) JWT access tokens and long-lived
/// (30 day default) opaque refresh tokens. Access tokens are intentionally
/// short-lived because they cannot be revoked before expiry without the
/// (not-yet-built) denylist — see docs/architecture/auth-service-architecture.md,
/// "Access vs refresh token lifetimes".
/// </summary>
public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options) => _options = options.Value;

    public AccessTokenResult GenerateAccessToken(User user, IReadOnlyCollection<string> roles)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_options.AccessTokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, ((DateTimeOffset)now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("first_name", user.FirstName),
            new("last_name", user.LastName)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

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
        // SHA-256 (not PBKDF2) is deliberate here: refresh tokens are already
        // 64 bytes of CSPRNG output, not a low-entropy human password, so a
        // fast, deterministic hash is correct — we need to look tokens up by
        // hash on every refresh, and a slow KDF would make that an expensive
        // per-request cost for no security benefit.
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
