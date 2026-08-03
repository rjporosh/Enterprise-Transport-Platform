namespace AuthService.Domain.Exceptions;

/// <summary>
/// Raised for an unknown, expired, or already-revoked/rotated refresh token.
/// A rotated (already-used) token being presented again is treated as a
/// possible token-theft signal — see RefreshTokenHandler, which revokes the
/// entire token family when this happens.
/// </summary>
public sealed class InvalidRefreshTokenException : DomainException
{
    public InvalidRefreshTokenException() : base("The refresh token is invalid, expired, or has already been used.") { }
}
