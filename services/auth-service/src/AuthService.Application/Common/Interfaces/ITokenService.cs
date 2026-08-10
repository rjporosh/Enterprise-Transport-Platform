using AuthService.Domain.Entities;

namespace AuthService.Application.Common.Interfaces;

public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAtUtc);
public sealed record RefreshTokenResult(string RawToken, string TokenHash, TimeSpan Lifetime);

public interface ITokenService
{
    AccessTokenResult GenerateAccessToken(User user, IReadOnlyCollection<string> roles);
    RefreshTokenResult GenerateRefreshToken();
    string HashRefreshToken(string rawToken);
    System.Security.Claims.ClaimsPrincipal? ValidateAccessToken(string accessToken);
}
