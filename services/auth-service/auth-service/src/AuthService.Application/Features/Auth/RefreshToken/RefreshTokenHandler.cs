using AuthService.Application.Common.Interfaces;
using AuthService.Application.Common.Models;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Auth.RefreshToken;

/// <summary>
/// Rotating refresh tokens: every successful refresh revokes the presented
/// token and issues a brand-new one (ReplacedByTokenId links them). If a
/// token that is already revoked gets presented again, that's a strong
/// signal of theft/replay — the entire token family for that user is
/// revoked and the caller must sign in again. See
/// docs/architecture/auth-service-architecture.md, "Refresh token rotation".
/// </summary>
public sealed class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, TokenPairDto>
{
    private readonly IAuthDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IDateTimeProvider _clock;
    private readonly IAuthMetrics _metrics;
    private readonly ILogger<RefreshTokenHandler> _logger;
    private readonly IAuditLogger _auditLogger;

    public RefreshTokenHandler(
        IAuthDbContext context,
        ITokenService tokenService,
        IDateTimeProvider clock,
        IAuthMetrics metrics,
        ILogger<RefreshTokenHandler> logger,
        IAuditLogger auditLogger)
    {
        _context = context;
        _tokenService = tokenService;
        _clock = clock;
        _metrics = metrics;
        _logger = logger;
        _auditLogger = auditLogger;
    }

    public async Task<TokenPairDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var presentedHash = _tokenService.HashRefreshToken(request.RawRefreshToken);

        var existing = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == presentedHash, cancellationToken);
        if (existing is null)
            throw new InvalidRefreshTokenException();

        if (existing.IsRevoked)
        {
            _logger.LogWarning("Revoked refresh token {TokenId} was presented again for user {UserId} — possible token theft; revoking entire token family.", existing.Id, existing.UserId);

            var activeFamily = await _context.RefreshTokens
                .Where(t => t.UserId == existing.UserId && t.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);
            foreach (var token in activeFamily)
                token.Revoke(now, request.IpAddress);

            await _context.SaveChangesAsync(cancellationToken);
            await _auditLogger.LogAsync(AuditAction.TokenReuseDetected, existing.UserId, null, success: false, request.IpAddress, request.UserAgent, $"Revoked {activeFamily.Count} active token(s) for this user.", cancellationToken);
            throw new InvalidRefreshTokenException();
        }

        if (existing.IsExpired(now))
            throw new InvalidRefreshTokenException();

        var user = await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == existing.UserId, cancellationToken);
        if (user is null)
            throw new InvalidRefreshTokenException();

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        var newRefreshTokenEntity = Domain.Entities.RefreshToken.Issue(user.Id, newRefreshToken.TokenHash, now, newRefreshToken.Lifetime, request.IpAddress);

        existing.Revoke(now, request.IpAddress, newRefreshTokenEntity.Id);
        _context.RefreshTokens.Add(newRefreshTokenEntity);

        await _context.SaveChangesAsync(cancellationToken);
        _metrics.RecordTokenRefresh();
        await _auditLogger.LogAsync(AuditAction.TokenRefresh, user.Id, user.Email, success: true, request.IpAddress, request.UserAgent, cancellationToken: cancellationToken);

        return new TokenPairDto(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            newRefreshToken.RawToken,
            newRefreshTokenEntity.ExpiresAtUtc,
            user.Id,
            user.Email,
            roles);
    }
}
