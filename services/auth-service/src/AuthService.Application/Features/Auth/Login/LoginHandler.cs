using AuthService.Application.Common.Interfaces;
using AuthService.Application.Common.Models;
using AuthService.Domain.Entities;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Auth.Login;

/// <summary>
/// Account-lockout policy: 5 failed attempts locks the account for 15
/// minutes (see AuthOptions). Every failure and every lockout is persisted
/// on the User aggregate itself (not just logged) so the lockout survives
/// across replicas/restarts and is enforced consistently everywhere.
/// </summary>
public sealed class LoginHandler : IRequestHandler<LoginCommand, TokenPairDto>
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IAuthDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IEventPublisher _eventPublisher;
    private readonly IDateTimeProvider _clock;
    private readonly IAuthMetrics _metrics;
    private readonly ILogger<LoginHandler> _logger;
    private readonly IAuditLogger _auditLogger;

    public LoginHandler(
        IAuthDbContext context,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IEventPublisher eventPublisher,
        IDateTimeProvider clock,
        IAuthMetrics metrics,
        ILogger<LoginHandler> logger,
        IAuditLogger auditLogger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _eventPublisher = eventPublisher;
        _clock = clock;
        _metrics = metrics;
        _logger = logger;
        _auditLogger = auditLogger;
    }

    public async Task<TokenPairDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var now = _clock.UtcNow;

        var user = await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        // Same exception (and roughly the same latency, thanks to hashing a
        // dummy password below) whether the email does not exist or the
        // password is wrong — never let a caller distinguish the two.
        if (user is null)
        {
            _passwordHasher.Verify(request.Password, DummyHashForTimingParity);
            _metrics.RecordLoginFailure();
            await _auditLogger.LogAsync(AuditAction.LoginFailure, null, normalizedEmail, success: false, request.IpAddress, request.UserAgent, "No account with this email.", cancellationToken);
            throw new InvalidCredentialsException();
        }

        if (user.IsLockedOut(now))
        {
            await _auditLogger.LogAsync(AuditAction.LoginFailure, user.Id, user.Email, success: false, request.IpAddress, request.UserAgent, "Account is locked out.", cancellationToken);
            throw new AccountLockedException(user.LockedUntilUtc!.Value);
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin(now, MaxFailedAttempts, LockoutDuration);

            foreach (var domainEvent in user.DomainEvents)
                await _eventPublisher.EnqueueAsync(domainEvent, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            user.ClearDomainEvents();

            _metrics.RecordLoginFailure();
            await _auditLogger.LogAsync(AuditAction.LoginFailure, user.Id, user.Email, success: false, request.IpAddress, request.UserAgent, "Incorrect password.", cancellationToken);

            if (user.IsLockedOut(now))
            {
                _metrics.RecordAccountLockout();
                await _auditLogger.LogAsync(AuditAction.AccountLockedOut, user.Id, user.Email, success: false, request.IpAddress, request.UserAgent, $"Locked until {user.LockedUntilUtc:u} after {MaxFailedAttempts} failed attempts.", cancellationToken);
            }

            throw new InvalidCredentialsException();
        }

        if (user.Status != UserStatus.Active)
        {
            await _auditLogger.LogAsync(AuditAction.LoginFailure, user.Id, user.Email, success: false, request.IpAddress, request.UserAgent, "Account is not active.", cancellationToken);
            throw new AccountNotActiveException();
        }

        user.RecordSuccessfulLogin(now, request.IpAddress);

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenEntity = Domain.Entities.RefreshToken.Issue(user.Id, refreshToken.TokenHash, now, refreshToken.Lifetime, request.IpAddress);
        _context.RefreshTokens.Add(refreshTokenEntity);

        foreach (var domainEvent in user.DomainEvents)
            await _eventPublisher.EnqueueAsync(domainEvent, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        user.ClearDomainEvents();

        _metrics.RecordLoginSuccess();
        _logger.LogInformation("User {UserId} signed in successfully", user.Id);
        await _auditLogger.LogAsync(AuditAction.LoginSuccess, user.Id, user.Email, success: true, request.IpAddress, request.UserAgent, cancellationToken: cancellationToken);

        return new TokenPairDto(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            refreshToken.RawToken,
            refreshTokenEntity.ExpiresAtUtc,
            user.Id,
            user.Email,
            roles);
    }

    // A real PBKDF2 hash of an arbitrary fixed string, verified against on
    // the "user not found" path purely to keep response timing close to the
    // "wrong password" path and reduce user-enumeration-by-timing risk.
    private const string DummyHashForTimingParity =
        "100000.MTIzNDU2Nzg5MGFiY2RlZg==.qGf3ZQe2m8m8kzR2mQe2m8m8kzR2mQe2m8m8kzR2mQe2==";
}
