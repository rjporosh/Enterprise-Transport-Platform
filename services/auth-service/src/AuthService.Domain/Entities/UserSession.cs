namespace AuthService.Domain.Entities;

public sealed class UserSession : Common.Entity
{
    public Guid UserId { get; private set; }
    public string SessionId { get; private set; } = default!;
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ExpiresAtUtc { get; private set; }
    public DateTimeOffset? LastActivityAtUtc { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }

    private UserSession() { }

    public UserSession(Guid id, Guid userId, string sessionId, string? ipAddress, string? userAgent, DateTimeOffset now, TimeSpan? lifetime = null)
        : base(id)
    {
        UserId = userId;
        SessionId = sessionId;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        CreatedAtUtc = now;
        ExpiresAtUtc = lifetime.HasValue ? now.Add(lifetime.Value) : null;
        LastActivityAtUtc = now;
    }

    public bool IsExpired(DateTimeOffset now) => ExpiresAtUtc.HasValue && now >= ExpiresAtUtc.Value;
    public bool IsActive(DateTimeOffset now) => !IsRevoked && !IsExpired(now);

    public void UpdateActivity(DateTimeOffset now)
    {
        LastActivityAtUtc = now;
    }

    public void Revoke(DateTimeOffset now)
    {
        IsRevoked = true;
        RevokedAtUtc = now;
    }
}
