using System.Security.Cryptography;
using System.Text;
using AuthService.Application.Common.Interfaces;
using AuthService.Domain.Entities;

namespace AuthService.UnitTests.TestSupport;

public sealed class FakeTokenService : ITokenService
{
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

    public AccessTokenResult GenerateAccessToken(User user, IReadOnlyCollection<string> roles) =>
        new($"fake-access-token-for-{user.Id}", DateTimeOffset.UtcNow.Add(AccessTokenLifetime));

    public RefreshTokenResult GenerateRefreshToken()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        return new RefreshTokenResult(raw, HashRefreshToken(raw), RefreshTokenLifetime);
    }

    public string HashRefreshToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    public System.Security.Claims.ClaimsPrincipal? ValidateAccessToken(string accessToken) => null;
}
