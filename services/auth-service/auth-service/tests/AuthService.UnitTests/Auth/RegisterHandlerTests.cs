using AuthService.Application.Common.Interfaces;
using AuthService.Application.Features.Auth.Register;
using AuthService.Domain.Entities;
using AuthService.Domain.Exceptions;
using AuthService.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace AuthService.UnitTests.Auth;

public class RegisterHandlerTests : IDisposable
{
    private readonly TestAuthDbContext _context;
    private readonly FakeEventPublisher _eventPublisher = new();
    private readonly FakeDateTimeProvider _clock = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeTokenService _tokenService = new();
    private readonly FakeAuditLogger _auditLogger = new();
    private readonly IAuthMetrics _metrics = Substitute.For<IAuthMetrics>();

    public RegisterHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestAuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestAuthDbContext(options);

        _context.Roles.Add(new Role(Guid.NewGuid(), Role.WellKnown.Customer, "Default role"));
        _context.SaveChanges();
    }

    private RegisterHandler CreateHandler() =>
        new(_context, _passwordHasher, _tokenService, _eventPublisher, _clock, _metrics, _auditLogger);

    [Fact]
    public async Task Handle_WithNewEmail_CreatesUser_AssignsCustomerRole_AndReturnsTokenPair()
    {
        var handler = CreateHandler();
        var command = new RegisterCommand("New.User@Example.com", "correct-horse-battery", "New", "User", null, "203.0.113.5", "xunit-agent");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Email.Should().Be("new.user@example.com");
        result.Roles.Should().ContainSingle(r => r == Role.WellKnown.Customer);

        var savedUser = await _context.Users.Include(u => u.UserRoles).FirstAsync();
        savedUser.Email.Should().Be("new.user@example.com");
        savedUser.UserRoles.Should().ContainSingle();

        _auditLogger.Entries.Should().ContainSingle(e => e.Action == Domain.Enums.AuditAction.Register && e.Success);
    }

    [Fact]
    public async Task Handle_WithDuplicateEmail_ThrowsUserAlreadyExistsException()
    {
        var existing = User.Register(Guid.NewGuid(), "taken@example.com", "hash", "Existing", "User", null, _clock.UtcNow);
        _context.Users.Add(existing);
        await _context.SaveChangesAsync();

        var handler = CreateHandler();
        var command = new RegisterCommand("Taken@Example.com", "correct-horse-battery", "New", "User", null, null, null);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UserAlreadyExistsException>();
    }

    public void Dispose() => _context.Dispose();
}
