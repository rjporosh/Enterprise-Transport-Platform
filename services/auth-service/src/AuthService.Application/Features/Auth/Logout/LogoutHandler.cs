using AuthService.Application.Common.Interfaces;
using AuthService.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Application.Features.Auth.Logout;

/// <summary>
/// Revokes the presented refresh token so it cannot be used to mint further
/// access tokens. Deliberately idempotent/no-op-on-not-found: logging out
/// with an already-invalid token should never be an error a client has to
/// handle specially — see docs/architecture/auth-service-architecture.md,
/// "Known gaps" for the "logout everywhere" / access-token-denylist variant
/// this does not yet cover (the ICacheService seam for it already exists).
/// </summary>
public sealed class LogoutHandler : IRequestHandler<LogoutCommand>
{
    private readonly IAuthDbContext _context;
    private readonly IDateTimeProvider _clock;
    private readonly ITokenService _tokenService;
    private readonly IAuditLogger _auditLogger;

    public LogoutHandler(IAuthDbContext context, IDateTimeProvider clock, ITokenService tokenService, IAuditLogger auditLogger)
    {
        _context = context;
        _clock = clock;
        _tokenService = tokenService;
        _auditLogger = auditLogger;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var hash = _tokenService.HashRefreshToken(request.RawRefreshToken);
        var token = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (token is null || token.IsRevoked)
            return;

        token.Revoke(_clock.UtcNow, revokedByIp: request.IpAddress);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(AuditAction.Logout, token.UserId, null, success: true, request.IpAddress, request.UserAgent, cancellationToken: cancellationToken);
    }
}
