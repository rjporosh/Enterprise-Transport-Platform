namespace AuthService.Application.Common.Models;

/// <summary>Returned by Register, Login, and RefreshToken — the shape a client needs to authenticate subsequent requests.</summary>
public sealed record TokenPairDto(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    Guid UserId,
    string Email,
    IReadOnlyCollection<string> Roles);
