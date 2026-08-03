using AuthService.Domain.Common;
using AuthService.Domain.Enums;
using AuthService.Domain.Events;

namespace AuthService.Domain.Entities;

/// <summary>
/// Aggregate root for identity. Owns credentials, lockout state, and role
/// assignments. Deliberately does NOT own RefreshToken rows as child
/// entities (they are persisted/queried independently by TokenId/hash) —
/// see docs/architecture/auth-service-architecture.md, "Why RefreshToken
/// is not inside the User aggregate".
/// </summary>
public sealed class User : AggregateRoot
{
    private readonly List<UserRole> _userRoles = new();

    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string? PhoneNumber { get; private set; }
    public UserStatus Status { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTimeOffset? LockedUntilUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? LastLoginAtUtc { get; private set; }

    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private User() { } // EF Core

    private User(Guid id, string email, string passwordHash, string firstName, string lastName, string? phoneNumber, DateTimeOffset now)
        : base(id)
    {
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        PhoneNumber = phoneNumber;
        CreatedAtUtc = now;

        // MVP: no transactional-email verification flow is built yet (see
        // docs/architecture/auth-service-architecture.md, "Known gaps"), so
        // new accounts are Active immediately rather than PendingVerification.
        // IsEmailVerified stays false until a future /verify-email endpoint
        // flips it — Status is intentionally decoupled from that flag so
        // adding real verification later doesn't require an auth-flow change.
        Status = UserStatus.Active;
        IsEmailVerified = false;
    }

    public static User Register(Guid id, string email, string passwordHash, string firstName, string lastName, string? phoneNumber, DateTimeOffset now)
    {
        var user = new User(id, email, passwordHash, firstName, lastName, phoneNumber, now);
        user.Raise(new UserRegisteredDomainEvent(user.Id, user.Email, user.FirstName, user.LastName));
        return user;
    }

    public void AssignRole(Guid roleId, DateTimeOffset now)
    {
        if (_userRoles.Any(ur => ur.RoleId == roleId)) return;
        _userRoles.Add(new UserRole(Id, roleId, now));
    }

    public bool IsLockedOut(DateTimeOffset now) => LockedUntilUtc.HasValue && LockedUntilUtc.Value > now;

    /// <summary>
    /// Increments the failed-attempt counter and locks the account once
    /// <paramref name="maxAttempts"/> is reached. Returns the raised lockout
    /// event, if any, so the caller (LoginHandler) can decide whether to
    /// enqueue it — kept explicit rather than auto-raised so a handler that
    /// is only checking lockout status (not attempting a real login) can't
    /// accidentally trigger a lockout notification.
    /// </summary>
    public void RecordFailedLogin(DateTimeOffset now, int maxAttempts, TimeSpan lockoutDuration)
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= maxAttempts)
        {
            LockedUntilUtc = now.Add(lockoutDuration);
            Raise(new UserLockedOutDomainEvent(Id, Email, LockedUntilUtc.Value));
        }
    }

    public void RecordSuccessfulLogin(DateTimeOffset now, string? ip)
    {
        FailedLoginAttempts = 0;
        LockedUntilUtc = null;
        LastLoginAtUtc = now;
        Raise(new UserLoggedInDomainEvent(Id, Email, ip));
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        Raise(new PasswordChangedDomainEvent(Id, Email));
    }

    public void Deactivate() => Status = UserStatus.Deactivated;

    public void Reactivate() => Status = UserStatus.Active;
}
