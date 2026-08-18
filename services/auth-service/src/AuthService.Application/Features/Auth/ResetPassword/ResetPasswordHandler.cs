using AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Auth.ResetPassword;

public sealed class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IAuthDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHistoryValidator _passwordHistoryValidator;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<ResetPasswordHandler> _logger;

    public ResetPasswordHandler(IAuthDbContext context, IPasswordHasher passwordHasher, ITokenService tokenService, IPasswordHistoryValidator passwordHistoryValidator, IAuditLogger auditLogger, ILogger<ResetPasswordHandler> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _passwordHistoryValidator = passwordHistoryValidator;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        // BUG FIX: the raw reset token is hashed with ITokenService.HashRefreshToken
        // (deterministic SHA-256) when it's issued in ForgotPasswordHandler and
        // stored on PasswordResetToken.TokenHash. This was looking it up with
        // IPasswordHasher.Hash instead, which is PBKDF2 with a fresh random salt
        // per call (by design, for user password storage) — so tokenHash here
        // never equaled the stored value and every reset-password request failed
        // with InvalidResetTokenException, for every user, 100% of the time.
        // Use the same hashing method that produced the stored hash.
        var tokenHash = _tokenService.HashRefreshToken(request.Token);
        var now = DateTimeOffset.UtcNow;

        var resetToken = await _context.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (resetToken is null || !resetToken.IsValid(now))
            throw new AuthService.Domain.Exceptions.InvalidResetTokenException();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == resetToken.UserId, cancellationToken);
        if (user is null)
            throw new AuthService.Domain.Exceptions.UserNotFoundException(resetToken.UserId);

        var newPasswordHash = _passwordHasher.Hash(request.NewPassword);
        var isReused = await _passwordHistoryValidator.IsPasswordReusedAsync(user.Id, request.NewPassword);
        if (isReused)
            throw new AuthService.Domain.Exceptions.PasswordHistoryException("Password cannot match one of the previous 3 passwords.");

        await _passwordHistoryValidator.RecordPasswordAsync(user.Id, newPasswordHash);
        user.ChangePassword(newPasswordHash);
        resetToken.MarkUsed(now);

        await _context.SaveChangesAsync(cancellationToken);
        await _auditLogger.LogAsync(AuthService.Domain.Enums.AuditAction.PasswordReset, user.Id, user.Email, success: true, request.IpAddress, request.UserAgent, cancellationToken: cancellationToken);
        _logger.LogInformation("Password reset completed for user {UserId}", user.Id);
    }
}
