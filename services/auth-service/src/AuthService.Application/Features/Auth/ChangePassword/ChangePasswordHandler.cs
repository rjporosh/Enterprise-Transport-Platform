using AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Auth.ChangePassword;

public sealed class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IAuthDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordHistoryValidator _passwordHistoryValidator;
    private readonly IEventPublisher _eventPublisher;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<ChangePasswordHandler> _logger;

    public ChangePasswordHandler(IAuthDbContext context, IPasswordHasher passwordHasher, IPasswordHistoryValidator passwordHistoryValidator, IEventPublisher eventPublisher, IAuditLogger auditLogger, ILogger<ChangePasswordHandler> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _passwordHistoryValidator = passwordHistoryValidator;
        _eventPublisher = eventPublisher;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null)
            throw new AuthService.Domain.Exceptions.UserNotFoundException(request.UserId);

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            await _auditLogger.LogAsync(AuthService.Domain.Enums.AuditAction.PasswordChanged, user.Id, user.Email, success: false, request.IpAddress, request.UserAgent, "Current password did not match.", cancellationToken);
            throw new AuthService.Domain.Exceptions.InvalidCredentialsException();
        }

        var isReused = await _passwordHistoryValidator.IsPasswordReusedAsync(user.Id, request.NewPassword);
        if (isReused)
            throw new AuthService.Domain.Exceptions.PasswordHistoryException("The new password cannot match one of the previous 3 passwords.");

        var newPasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.ChangePassword(newPasswordHash);
        await _passwordHistoryValidator.RecordPasswordAsync(user.Id, newPasswordHash);

        foreach (var domainEvent in user.DomainEvents)
            await _eventPublisher.EnqueueAsync(domainEvent, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        user.ClearDomainEvents();

        await _auditLogger.LogAsync(AuthService.Domain.Enums.AuditAction.PasswordChanged, user.Id, user.Email, success: true, request.IpAddress, request.UserAgent, cancellationToken: cancellationToken);
        _logger.LogInformation("Password changed for user {UserId}", user.Id);
    }
}
