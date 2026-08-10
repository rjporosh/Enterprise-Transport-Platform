using AuthService.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Auth.ForgotPassword;

public sealed class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IAuthDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<ForgotPasswordHandler> _logger;

    public ForgotPasswordHandler(IAuthDbContext context, ITokenService tokenService, IAuditLogger auditLogger, ILogger<ForgotPasswordHandler> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is not null)
        {
            var now = DateTimeOffset.UtcNow;
            var rawToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            var tokenHash = _tokenService.HashRefreshToken(rawToken);

            var resetToken = new AuthService.Domain.Entities.PasswordResetToken(
                Guid.NewGuid(), user.Id, tokenHash, now, TimeSpan.FromHours(1), request.IpAddress);
            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(AuthService.Domain.Enums.AuditAction.ForgotPassword, user.Id, user.Email, success: true, request.IpAddress, request.UserAgent, cancellationToken: cancellationToken);
            _logger.LogInformation("Password reset token issued for user {UserId}", user.Id);
        }
        else
        {
            await _auditLogger.LogAsync(AuthService.Domain.Enums.AuditAction.ForgotPassword, null, normalizedEmail, success: false, request.IpAddress, request.UserAgent, "No account with this email.", cancellationToken);
        }
    }
}
