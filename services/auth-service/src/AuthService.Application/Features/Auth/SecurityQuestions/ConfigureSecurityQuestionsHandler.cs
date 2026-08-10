using AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Auth.SecurityQuestions;

public sealed class ConfigureSecurityQuestionsHandler : IRequestHandler<ConfigureSecurityQuestionsCommand>
{
    private readonly IAuthDbContext _context;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<ConfigureSecurityQuestionsHandler> _logger;
    private const int MaxQuestions = 5;

    public ConfigureSecurityQuestionsHandler(IAuthDbContext context, IAuditLogger auditLogger, ILogger<ConfigureSecurityQuestionsHandler> logger)
    {
        _context = context;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task Handle(ConfigureSecurityQuestionsCommand request, CancellationToken cancellationToken)
    {
        if (request.QuestionAnswers.Count < 3 || request.QuestionAnswers.Count > MaxQuestions)
            throw new AuthService.Domain.Exceptions.InvalidSecurityAnswerException($"You must configure between 3 and {MaxQuestions} security questions.");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null)
            throw new AuthService.Domain.Exceptions.UserNotFoundException(request.UserId);

        var existingAnswers = await _context.SecurityAnswers
            .Where(sa => sa.UserId == request.UserId)
            .ToListAsync(cancellationToken);
        _context.SecurityAnswers.RemoveRange(existingAnswers);

        var existingQuestions = await _context.UserSecurityQuestions
            .Where(usq => usq.UserId == request.UserId)
            .ToListAsync(cancellationToken);
        _context.UserSecurityQuestions.RemoveRange(existingQuestions);

        var now = DateTimeOffset.UtcNow;
        foreach (var (questionId, answer) in request.QuestionAnswers)
        {
            var securityAnswer = new AuthService.Domain.Entities.SecurityAnswer(Guid.NewGuid(), request.UserId, questionId, answer, now);
            _context.SecurityAnswers.Add(securityAnswer);
            _context.UserSecurityQuestions.Add(new AuthService.Domain.Entities.UserSecurityQuestion(request.UserId, questionId, now));
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _auditLogger.LogAsync(AuditAction.SecurityQuestionConfigured, user.Id, user.Email, success: true, request.IpAddress, request.UserAgent, cancellationToken: cancellationToken);
        _logger.LogInformation("Security questions configured for user {UserId}", request.UserId);
    }
}
