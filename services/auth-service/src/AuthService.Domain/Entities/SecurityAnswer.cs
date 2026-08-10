namespace AuthService.Domain.Entities;

public sealed class SecurityAnswer : Common.Entity
{
    public Guid UserId { get; private set; }
    public Guid SecurityQuestionId { get; private set; }
    public string AnswerHash { get; private set; } = default!;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private SecurityAnswer() { }

    public SecurityAnswer(Guid id, Guid userId, Guid securityQuestionId, string answerPlainText, DateTimeOffset now)
        : base(id)
    {
        UserId = userId;
        SecurityQuestionId = securityQuestionId;
        AnswerHash = ComputeHash(NormalizeAnswer(answerPlainText));
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public bool Verify(string answerPlainText)
    {
        var normalized = NormalizeAnswer(answerPlainText);
        return string.Equals(AnswerHash, ComputeHash(normalized), StringComparison.Ordinal);
    }

    public void UpdateAnswer(string newAnswerPlainText, DateTimeOffset now)
    {
        AnswerHash = ComputeHash(NormalizeAnswer(newAnswerPlainText));
        UpdatedAtUtc = now;
    }

    private static string NormalizeAnswer(string answer)
    {
        return answer.Trim().ToLowerInvariant();
    }

    private static string ComputeHash(string normalizedAnswer)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(normalizedAnswer);
        return Convert.ToHexString(sha256.ComputeHash(bytes));
    }
}
