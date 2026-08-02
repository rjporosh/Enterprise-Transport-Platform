using AuthService.Application.Common.Interfaces;
using AuthService.Application.Features.Auth.Login;
using AuthService.Domain.Entities;
using AuthService.Domain.Exceptions;
using AuthService.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace AuthService.UnitTests.Auth;

public class LoginHandlerTests : IDisposable
{
    private readonly TestAuthDbContext _context;
    private readonly FakeEventPublisher _eventPublisher = new();
    private readonly FakeDateTimeProvider _clock = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeTokenService _tokenService = new();
    private readonly FakeAuditLogger _auditLogger = new();
    private readonly IAuthMetrics _metrics = Substitute.For<IAuthMetrics>();
    private readonly Role _customerRole;

    public LoginHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestAuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestAuthDbContext(options);

        _customerRole = new Role(Guid.NewGuid(), Role.WellKnown.Customer, "Default role");
        _context.Roles.Add(_customerRole);
        _context.SaveChanges();
    }

    private LoginHandler CreateHandler() =>
        new(_context, _passwordHasher, _tokenService, _eventPublisher, _clock, _metrics, NullLogger<LoginHandler>.Instance, _auditLogger);

    private User SeedUser(string email, string password)
    {
        var user = User.Register(Guid.NewGuid(), email, _passwordHasher.Hash(password), "Jane", "Doe", null, _clock.UtcNow);
        user.AssignRole(_customerRole.Id, _clock.UtcNow);
        user.ClearDomainEvents();
        _context.Users.Add(user);
        _context.SaveChanges();
        return user;
    }

    [Fact]
    public async Task Handle_WithCorrectCredentials_ReturnsTokenPair_AndRecordsSuccessAudit()
    {
        SeedUser("jane@example.com", "correct-horse-battery");
        var handler = CreateHandler();

        var result = await handler.Handle(new LoginCommand("jane@example.com", "correct-horse-battery", "203.0.113.7", "xunit-agent"), CancellationToken.None);

        result.Email.Should().Be("jane@example.com");
        result.Roles.Should().Contain(Role.WellKnown.Customer);
        _auditLogger.Entries.Should().ContainSingle(e => e.Action == Domain.Enums.AuditAction.LoginSuccess);
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ThrowsInvalidCredentials_AndIncrementsFailedAttempts()
    {
        var user = SeedUser("jane@example.com", "correct-horse-battery");
        var handler = CreateHandler();

        var act = async () => await handler.Handle(new LoginCommand("jane@example.com", "wrong-password", null, null), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();

        var reloaded = await _context.Users.FirstAsync(u => u.Id == user.Id);
        reloaded.FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithUnknownEmail_ThrowsInvalidCredentials_WithoutRevealingWhichPartWasWrong()
    {
        var handler = CreateHandler();

        var act = async () => await handler.Handle(new LoginCommand("nobody@example.com", "anything", null, null), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
        _auditLogger.Entries.Should().ContainSingle(e => e.Action == Domain.Enums.AuditAction.LoginFailure && e.UserId == null);
    }

    [Fact]
    public async Task Handle_AfterFiveFailedAttempts_LocksAccount_AndSubsequentLoginThrowsAccountLocked()
    {
        SeedUser("jane@example.com", "correct-horse-battery");
        var handler = CreateHandler();

        for (var i = 0; i < 5; i++)
        {
            var act = async () => await handler.Handle(new LoginCommand("jane@example.com", "wrong-password", null, null), CancellationToken.None);
            await act.Should().ThrowAsync<InvalidCredentialsException>();
        }

        var lockedAct = async () => await handler.Handle(new LoginCommand("jane@example.com", "correct-horse-battery", null, null), CancellationToken.None);
        await lockedAct.Should().ThrowAsync<AccountLockedException>();

        _auditLogger.Entries.Should().Contain(e => e.Action == Domain.Enums.AuditAction.AccountLockedOut);
    }

    public void Dispose() => _context.Dispose();
}
