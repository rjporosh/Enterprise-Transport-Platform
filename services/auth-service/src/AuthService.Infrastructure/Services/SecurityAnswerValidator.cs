using AuthService.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Services;

public sealed class SecurityAnswerValidator : ISecurityAnswerValidator
{
    private readonly IAuthDbContext _context;
    private readonly ILogger<SecurityAnswerValidator> _logger;

    public SecurityAnswerValidator(IAuthDbContext context, ILogger<SecurityAnswerValidator> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> VerifyAnswersAsync(Guid userId, IDictionary<Guid, string> questionAnswers, CancellationToken cancellationToken = default)
    {
        var configuredQuestions = await _context.UserSecurityQuestions
            .Where(usq => usq.UserId == userId)
            .Select(usq => usq.SecurityQuestionId)
            .ToListAsync(cancellationToken);

        if (configuredQuestions.Count == 0)
            throw new AuthService.Domain.Exceptions.SecurityQuestionsNotConfiguredException();

        if (questionAnswers.Count != configuredQuestions.Count)
            throw new AuthService.Domain.Exceptions.InvalidSecurityAnswerException("One or more security answers are incorrect.");

        foreach (var (questionId, answer) in questionAnswers)
        {
            var securityAnswer = await _context.SecurityAnswers
                .FirstOrDefaultAsync(sa => sa.UserId == userId && sa.SecurityQuestionId == questionId, cancellationToken);

            if (securityAnswer is null || !securityAnswer.Verify(answer))
            {
                _logger.LogWarning("Security answer verification failed for user {UserId} on question {QuestionId}", userId, questionId);
                throw new AuthService.Domain.Exceptions.InvalidSecurityAnswerException("One or more security answers are incorrect.");
            }
        }

        return true;
    }
}
