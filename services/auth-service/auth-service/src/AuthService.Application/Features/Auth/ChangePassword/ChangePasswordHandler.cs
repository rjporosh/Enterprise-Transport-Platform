using AuthService.Application.Common.Interfaces;
using AuthService.Domain.Enums;
using AuthService.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Application.Features.Auth.ChangePassword;

/// <summary>
/// Requires the caller to already hold a valid access token (UserId comes
/// from ICurrentUser at the API layer, not from the request body) AND know
/// the current password — defense in depth against a stolen-but-not-yet-
/// expired access token being used to lock the real owner out permanently.
/// </summary>
public sealed class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IAuthDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEventPublisher _eventPublisher;
    private readonly IAuditLogger _auditLogger;

    public ChangePasswordHandler(IAuthDbContext context, IPasswordHasher passwordHasher, IEventPublisher eventPublisher, IAuditLogger auditLogger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _eventPublisher = eventPublisher;
        _auditLogger = auditLogger;
    }

    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null)
            throw new UserNotFoundException(request.UserId);

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            await _auditLogger.LogAsync(AuditAction.PasswordChanged, user.Id, user.Email, success: false, request.IpAddress, request.UserAgent, "Current password did not match.", cancellationToken);
            throw new InvalidCredentialsException();
        }

        user.ChangePassword(_passwordHasher.Hash(request.NewPassword));

        foreach (var domainEvent in user.DomainEvents)
            await _eventPublisher.EnqueueAsync(domainEvent, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        user.ClearDomainEvents();

        await _auditLogger.LogAsync(AuditAction.PasswordChanged, user.Id, user.Email, success: true, request.IpAddress, request.UserAgent, cancellationToken: cancellationToken);
    }
}
