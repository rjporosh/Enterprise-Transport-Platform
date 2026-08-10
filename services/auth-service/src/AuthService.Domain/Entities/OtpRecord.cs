namespace AuthService.Domain.Entities;

public sealed class OtpRecord : Common.Entity
{
    public Guid UserId { get; private set; }
    public string CodeHash { get; private set; } = default!;
    public string Channel { get; private set; } = default!;
    public string Destination { get; private set; } = default!;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? VerifiedAtUtc { get; private set; }
    public int AttemptCount { get; private set; }
    public int ResendCount { get; private set; }
    public bool IsUsed { get; private set; }
    public string? IpAddress { get; private set; }

    private OtpRecord() { }

    public OtpRecord(Guid id, Guid userId, string codeHash, string channel, string destination, DateTimeOffset now, TimeSpan lifetime, string? ipAddress)
        : base(id)
    {
        UserId = userId;
        CodeHash = codeHash;
        Channel = channel;
        Destination = destination;
        ExpiresAtUtc = now.Add(lifetime);
        CreatedAtUtc = now;
        IpAddress = ipAddress;
    }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAtUtc;
    public bool IsVerified => VerifiedAtUtc.HasValue;

    public bool CanAttempt(DateTimeOffset now, int maxAttempts)
    {
        return !IsUsed && !IsExpired(now) && AttemptCount < maxAttempts;
    }

    public void MarkAttempt()
    {
        AttemptCount++;
    }

    public void MarkVerified(DateTimeOffset now)
    {
        IsUsed = true;
        VerifiedAtUtc = now;
    }

    public void IncrementResend() => ResendCount++;
}
