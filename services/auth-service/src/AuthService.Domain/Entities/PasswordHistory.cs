namespace AuthService.Domain.Entities;

public sealed class PasswordHistory : Common.Entity
{
    public Guid UserId { get; private set; }
    public string PasswordHash { get; private set; } = default!;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private PasswordHistory() { }

    public PasswordHistory(Guid id, Guid userId, string passwordHash, DateTimeOffset now)
        : base(id)
    {
        UserId = userId;
        PasswordHash = passwordHash;
        CreatedAtUtc = now;
    }
}
