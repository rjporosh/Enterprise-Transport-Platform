namespace AuthService.Domain.Entities;

public sealed class UserClaim
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = default!;
    public string Type { get; private set; } = default!;
    public string Value { get; private set; } = default!;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private UserClaim() { }

    public UserClaim(Guid userId, string type, string value, DateTimeOffset createdAtUtc)
    {
        UserId = userId;
        Type = type;
        Value = value;
        CreatedAtUtc = createdAtUtc;
    }
}
