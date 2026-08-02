using AuthService.Domain.Entities;
using AuthService.Domain.Events;
using FluentAssertions;
using Xunit;

namespace AuthService.UnitTests.Users;

public class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Register_CreatesActiveUnverifiedUser_AndRaisesUserRegisteredEvent()
    {
        var user = User.Register(Guid.NewGuid(), "Jane.Doe@Example.com", "hashed", "Jane", "Doe", null, Now);

        user.Email.Should().Be("jane.doe@example.com"); // normalized
        user.IsEmailVerified.Should().BeFalse();
        user.Status.Should().Be(Domain.Enums.UserStatus.Active);
        user.DomainEvents.Should().ContainSingle(e => e is UserRegisteredDomainEvent);
    }

    [Fact]
    public void RecordFailedLogin_LocksAccount_AfterMaxAttemptsReached()
    {
        var user = User.Register(Guid.NewGuid(), "jane@example.com", "hashed", "Jane", "Doe", null, Now);
        user.ClearDomainEvents();

        for (var i = 0; i < 4; i++)
            user.RecordFailedLogin(Now, maxAttempts: 5, lockoutDuration: TimeSpan.FromMinutes(15));

        user.IsLockedOut(Now).Should().BeFalse();

        user.RecordFailedLogin(Now, maxAttempts: 5, lockoutDuration: TimeSpan.FromMinutes(15));

        user.IsLockedOut(Now).Should().BeTrue();
        user.LockedUntilUtc.Should().Be(Now.AddMinutes(15));
        user.DomainEvents.Should().ContainSingle(e => e is UserLockedOutDomainEvent);
    }

    [Fact]
    public void RecordSuccessfulLogin_ResetsFailedAttempts_AndClearsLockout()
    {
        var user = User.Register(Guid.NewGuid(), "jane@example.com", "hashed", "Jane", "Doe", null, Now);
        user.RecordFailedLogin(Now, maxAttempts: 5, lockoutDuration: TimeSpan.FromMinutes(15));
        user.RecordFailedLogin(Now, maxAttempts: 5, lockoutDuration: TimeSpan.FromMinutes(15));
        user.ClearDomainEvents();

        user.RecordSuccessfulLogin(Now.AddMinutes(1), "203.0.113.10");

        user.FailedLoginAttempts.Should().Be(0);
        user.LockedUntilUtc.Should().BeNull();
        user.LastLoginAtUtc.Should().Be(Now.AddMinutes(1));
        user.DomainEvents.Should().ContainSingle(e => e is UserLoggedInDomainEvent);
    }

    [Fact]
    public void ChangePassword_UpdatesHash_AndRaisesPasswordChangedEvent()
    {
        var user = User.Register(Guid.NewGuid(), "jane@example.com", "old-hash", "Jane", "Doe", null, Now);
        user.ClearDomainEvents();

        user.ChangePassword("new-hash");

        user.PasswordHash.Should().Be("new-hash");
        user.DomainEvents.Should().ContainSingle(e => e is PasswordChangedDomainEvent);
    }
}
