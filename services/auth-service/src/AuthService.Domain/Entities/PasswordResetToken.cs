namespace AuthService.Domain.Entities;

public sealed class PasswordResetToken : Common.Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UsedAtUtc { get; private set; }
    public string? CreatedByIp { get; private set; }
    public bool IsUsed => UsedAtUtc.HasValue;

    private PasswordResetToken() { }

    public PasswordResetToken(Guid id, Guid userId, string tokenHash, DateTimeOffset now, TimeSpan lifetime, string? createdByIp)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = now.Add(lifetime);
        CreatedAtUtc = now;
        CreatedByIp = createdByIp;
    }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAtUtc;
    public bool IsValid(DateTimeOffset now) => !IsUsed && !IsExpired(now);

    public void MarkUsed(DateTimeOffset now)
    {
        UsedAtUtc = now;
    }
}
