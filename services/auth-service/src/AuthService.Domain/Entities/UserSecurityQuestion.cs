namespace AuthService.Domain.Entities;

public sealed class UserSecurityQuestion
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = default!;
    public Guid SecurityQuestionId { get; private set; }
    public SecurityQuestion SecurityQuestion { get; private set; } = default!;
    public DateTimeOffset ConfiguredAtUtc { get; private set; }

    private UserSecurityQuestion() { }

    public UserSecurityQuestion(Guid userId, Guid securityQuestionId, DateTimeOffset configuredAtUtc)
    {
        UserId = userId;
        SecurityQuestionId = securityQuestionId;
        ConfiguredAtUtc = configuredAtUtc;
    }
}
