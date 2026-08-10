namespace AuthService.Application.Common.Interfaces;

public interface ISecurityAnswerValidator
{
    Task<bool> VerifyAnswersAsync(Guid userId, IDictionary<Guid, string> questionAnswers, CancellationToken cancellationToken = default);
}
