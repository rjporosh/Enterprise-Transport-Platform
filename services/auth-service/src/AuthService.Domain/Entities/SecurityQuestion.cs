using System.Security.Cryptography;

namespace AuthService.Domain.Entities;

public sealed class SecurityQuestion : Common.Entity
{
    public string QuestionText { get; private set; } = default!;
    public bool IsActive { get; private set; }

    private SecurityQuestion() { }

    public SecurityQuestion(Guid id, string questionText) : base(id)
    {
        QuestionText = questionText;
        IsActive = true;
    }

    public void Update(string questionText)
    {
        QuestionText = questionText;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
