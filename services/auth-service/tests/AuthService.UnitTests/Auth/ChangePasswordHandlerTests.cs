using AuthService.Application.Features.Auth.ChangePassword;
using AuthService.Domain.Enums;
using AuthService.Domain.Entities;
using AuthService.Domain.Exceptions;
using AuthService.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AuthService.UnitTests.Auth;

public class ChangePasswordHandlerTests : IDisposable
{
    private readonly TestAuthDbContext _context;
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeEventPublisher _eventPublisher = new();
    private readonly FakeDateTimeProvider _clock = new();
    private readonly FakeAuditLogger _auditLogger = new();
    private ChangePasswordHandler _handler = default!;
    private User _user = default!;

    public ChangePasswordHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestAuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestAuthDbContext(options);
        Setup();
    }

    private void Setup()
    {
        _handler = new ChangePasswordHandler(
            _context,
            _passwordHasher,
            new AuthService.Infrastructure.Services.PasswordHistoryValidator(_context, _passwordHasher, NullLogger<AuthService.Infrastructure.Services.PasswordHistoryValidator>.Instance),
            _eventPublisher,
            _auditLogger,
            NullLogger<ChangePasswordHandler>.Instance);

        _user = User.Register(Guid.NewGuid(), "cp@example.com", _passwordHasher.Hash("OldPass123!"), "Change", "Password", null, _clock.UtcNow);
        _context.Users.Add(_user);
        _context.SaveChanges();
    }

    [Fact]
    public async Task ChangePassword_WithCorrectCurrentPassword_ChangesSuccessfully()
    {
        await _handler.Handle(new ChangePasswordCommand(_user.Id, "OldPass123!", "NewPass456!", null, null), CancellationToken.None);
        _auditLogger.Entries.Should().ContainSingle(e => e.Action == AuditAction.PasswordChanged);
        _passwordHasher.Verify("NewPass456!", _user.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_ThrowsInvalidCredentialsException()
    {
        var act = async () => await _handler.Handle(new ChangePasswordCommand(_user.Id, "WrongPass", "NewPass456!", null, null), CancellationToken.None);
        await act.Should().ThrowAsync<AuthService.Domain.Exceptions.InvalidCredentialsException>();
    }

    [Fact]
    public async Task ChangePassword_WithReusedPassword_ThrowsPasswordHistoryException()
    {
        await _handler.Handle(new ChangePasswordCommand(_user.Id, "OldPass123!", "NewPass456!", null, null), CancellationToken.None);
        _context.Entry(_user).State = EntityState.Detached;
        _user = await _context.Users.FirstAsync(u => u.Id == _user.Id);

        await _handler.Handle(new ChangePasswordCommand(_user.Id, "NewPass456!", "AnotherPass!", null, null), CancellationToken.None);
        _context.Entry(_user).State = EntityState.Detached;
        _user = await _context.Users.FirstAsync(u => u.Id == _user.Id);

        var act = async () => await _handler.Handle(new ChangePasswordCommand(_user.Id, "AnotherPass!", "NewPass456!", null, null), CancellationToken.None);
        await act.Should().ThrowAsync<AuthService.Domain.Exceptions.PasswordHistoryException>();
    }

    [Fact]
    public async Task ChangePassword_AfterThreeChanges_OldestPasswordCanBeReused()
    {
        await _handler.Handle(new ChangePasswordCommand(_user.Id, "OldPass123!", "Pass1!", null, null), CancellationToken.None);
        _context.Entry(_user).State = EntityState.Detached;
        _user = await _context.Users.FirstAsync(u => u.Id == _user.Id);

        await _handler.Handle(new ChangePasswordCommand(_user.Id, "Pass1!", "Pass2!", null, null), CancellationToken.None);
        _context.Entry(_user).State = EntityState.Detached;
        _user = await _context.Users.FirstAsync(u => u.Id == _user.Id);

        await _handler.Handle(new ChangePasswordCommand(_user.Id, "Pass2!", "Pass3!", null, null), CancellationToken.None);
        _context.Entry(_user).State = EntityState.Detached;
        _user = await _context.Users.FirstAsync(u => u.Id == _user.Id);

        await _handler.Handle(new ChangePasswordCommand(_user.Id, "Pass3!", "OldPass123!", null, null), CancellationToken.None);
        _passwordHasher.Verify("OldPass123!", _user.PasswordHash).Should().BeTrue();
    }

    public void Dispose() => _context.Dispose();
}
