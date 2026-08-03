using AuthService.Domain.Entities;

namespace AuthService.Application.Common.Interfaces;

public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAtUtc);

/// <summary>Raw (unhashed) refresh token value + how long it is valid for. Only the caller ever sees the raw value.</summary>
public sealed record RefreshTokenResult(string RawToken, string TokenHash, TimeSpan Lifetime);

public interface ITokenService
{
    AccessTokenResult GenerateAccessToken(User user, IReadOnlyCollection<string> roles);

    RefreshTokenResult GenerateRefreshToken();

    /// <summary>Hashes a raw refresh token the same way GenerateRefreshToken did, so a presented token can be looked up by hash.</summary>
    string HashRefreshToken(string rawToken);
}
