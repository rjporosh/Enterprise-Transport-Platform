using AuthService.Application.Common.Interfaces;
using AuthService.Application.Common.Models;
using AuthService.Domain.Entities;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Application.Features.Auth.Register;

/// <summary>
/// Creates the account, assigns the default Customer role, issues an initial
/// access/refresh token pair (so the client is signed in immediately after
/// registering — no separate login round-trip), and enqueues
/// UserRegisteredDomainEvent to the outbox for Notification Service.
/// </summary>
public sealed class RegisterHandler : IRequestHandler<RegisterCommand, TokenPairDto>
{
    private readonly IAuthDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IEventPublisher _eventPublisher;
    private readonly IDateTimeProvider _clock;
    private readonly IAuthMetrics _metrics;
    private readonly IAuditLogger _auditLogger;

    public RegisterHandler(
        IAuthDbContext context,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IEventPublisher eventPublisher,
        IDateTimeProvider clock,
        IAuthMetrics metrics,
        IAuditLogger auditLogger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _eventPublisher = eventPublisher;
        _clock = clock;
        _metrics = metrics;
        _auditLogger = auditLogger;
    }

    public async Task<TokenPairDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var exists = await _context.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (exists)
            throw new UserAlreadyExistsException(normalizedEmail);

        var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == Role.WellKnown.Customer, cancellationToken);
        if (customerRole is null)
            throw new InvalidOperationException($"Well-known role '{Role.WellKnown.Customer}' is not seeded. Run migrations.");

        var now = _clock.UtcNow;
        var passwordHash = _passwordHasher.Hash(request.Password);

        var user = User.Register(Guid.NewGuid(), normalizedEmail, passwordHash, request.FirstName, request.LastName, request.PhoneNumber, now);
        user.AssignRole(customerRole.Id, now);

        _context.Users.Add(user);

        foreach (var domainEvent in user.DomainEvents)
            await _eventPublisher.EnqueueAsync(domainEvent, cancellationToken);

        var accessToken = _tokenService.GenerateAccessToken(user, new[] { customerRole.Name });
        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenEntity = Domain.Entities.RefreshToken.Issue(user.Id, refreshToken.TokenHash, now, refreshToken.Lifetime, request.IpAddress);
        _context.RefreshTokens.Add(refreshTokenEntity);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Two concurrent registrations for the same email both passed the
            // AnyAsync pre-check above and raced to insert — the unique index
            // on users.email (see UserConfiguration) let exactly one through
            // and threw on the loser. `await` isn't allowed in a catch-filter
            // expression (CS7094), so the check has to happen in the catch
            // body: re-query for a *different* row with this email, and only
            // swallow the exception as "someone else won the race" if one
            // exists. Otherwise this was some other DB failure — rethrow it
            // rather than mask it as a false 409. See tests/load/README.md,
            // "Register race" for the load test that exercises this path.
            var wonByAnotherRequest = await _context.Users.AnyAsync(u => u.Id != user.Id && u.Email == normalizedEmail, cancellationToken);
            if (!wonByAnotherRequest)
                throw;

            throw new UserAlreadyExistsException(normalizedEmail);
        }
        user.ClearDomainEvents();

        _metrics.RecordRegistration();
        await _auditLogger.LogAsync(AuditAction.Register, user.Id, user.Email, success: true, request.IpAddress, request.UserAgent, cancellationToken: cancellationToken);

        return new TokenPairDto(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            refreshToken.RawToken,
            refreshTokenEntity.ExpiresAtUtc,
            user.Id,
            user.Email,
            new[] { customerRole.Name });
    }
}
