namespace AuthService.Domain.Entities;

/// <summary>
/// A single-use, rotating refresh token. Only the SHA-256 hash of the raw
/// token value is ever persisted — the raw value is returned to the client
/// exactly once, at issuance, and cannot be recovered from the database.
/// </summary>
public sealed class RefreshToken : Common.Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public string? CreatedByIp { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? RevokedByIp { get; private set; }

    /// <summary>Points at the token that replaced this one, when rotated — lets us walk and revoke an entire token family if reuse is detected.</summary>
    public Guid? ReplacedByTokenId { get; private set; }

    private RefreshToken() { } // EF Core

    private RefreshToken(Guid id, Guid userId, string tokenHash, DateTimeOffset now, TimeSpan lifetime, string? createdByIp)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        CreatedAtUtc = now;
        ExpiresAtUtc = now.Add(lifetime);
        CreatedByIp = createdByIp;
    }

    public static RefreshToken Issue(Guid userId, string tokenHash, DateTimeOffset now, TimeSpan lifetime, string? createdByIp) =>
        new(Guid.NewGuid(), userId, tokenHash, now, lifetime, createdByIp);

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAtUtc;

    public bool IsRevoked => RevokedAtUtc is not null;

    public bool IsActive(DateTimeOffset now) => !IsRevoked && !IsExpired(now);

    public void Revoke(DateTimeOffset now, string? revokedByIp, Guid? replacedByTokenId = null)
    {
        RevokedAtUtc = now;
        RevokedByIp = revokedByIp;
        ReplacedByTokenId = replacedByTokenId;
    }
}
