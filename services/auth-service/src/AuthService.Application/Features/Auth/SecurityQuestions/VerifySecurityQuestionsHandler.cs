using AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Auth.SecurityQuestions;

public sealed class VerifySecurityQuestionsHandler : IRequestHandler<VerifySecurityQuestionsCommand>
{
    private readonly ISecurityAnswerValidator _securityAnswerValidator;
    private readonly IAuthDbContext _context;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<VerifySecurityQuestionsHandler> _logger;

    public VerifySecurityQuestionsHandler(ISecurityAnswerValidator securityAnswerValidator, IAuthDbContext context, IAuditLogger auditLogger, ILogger<VerifySecurityQuestionsHandler> logger)
    {
        _securityAnswerValidator = securityAnswerValidator;
        _context = context;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task Handle(VerifySecurityQuestionsCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null)
            throw new AuthService.Domain.Exceptions.UserNotFoundException(request.UserId);

        var isValid = await _securityAnswerValidator.VerifyAnswersAsync(request.UserId, request.QuestionAnswers, cancellationToken);
        if (!isValid)
        {
            await _auditLogger.LogAsync(AuditAction.SecurityQuestionFailed, user.Id, user.Email, success: false, request.IpAddress, request.UserAgent, "Incorrect security answers.", cancellationToken);
            throw new AuthService.Domain.Exceptions.InvalidSecurityAnswerException("One or more security answers are incorrect.");
        }

        await _auditLogger.LogAsync(AuditAction.SecurityQuestionVerified, user.Id, user.Email, success: true, request.IpAddress, request.UserAgent, cancellationToken: cancellationToken);
        _logger.LogInformation("Security questions verified for user {UserId}", request.UserId);
    }
}
