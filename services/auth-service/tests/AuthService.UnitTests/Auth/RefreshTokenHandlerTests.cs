using AuthService.Application.Common.Interfaces;
using AuthService.Application.Features.Auth.RefreshToken;
using AuthService.Domain.Entities;
using AuthService.Domain.Exceptions;
using AuthService.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace AuthService.UnitTests.Auth;

public class RefreshTokenHandlerTests : IDisposable
{
    private readonly TestAuthDbContext _context;
    private readonly FakeDateTimeProvider _clock = new();
    private readonly FakeTokenService _tokenService = new();
    private readonly FakeAuditLogger _auditLogger = new();
    private readonly IAuthMetrics _metrics = Substitute.For<IAuthMetrics>();
    private readonly Role _customerRole;
    private User _user = default!;

    public RefreshTokenHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestAuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestAuthDbContext(options);

        _customerRole = new Role(Guid.NewGuid(), Role.WellKnown.Customer, "Default role");
        _context.Roles.Add(_customerRole);

        _user = User.Register(Guid.NewGuid(), "jane@example.com", "hash", "Jane", "Doe", null, _clock.UtcNow);
        _user.AssignRole(_customerRole.Id, _clock.UtcNow);
        _user.ClearDomainEvents();
        _context.Users.Add(_user);
        _context.SaveChanges();
    }

    private RefreshTokenHandler CreateHandler() =>
        new(_context, _tokenService, _clock, _metrics, NullLogger<RefreshTokenHandler>.Instance, _auditLogger);

    private string SeedActiveRefreshToken()
    {
        var raw = "raw-refresh-token-value";
        var entity = Domain.Entities.RefreshToken.Issue(_user.Id, _tokenService.HashRefreshToken(raw), _clock.UtcNow, TimeSpan.FromDays(30), "203.0.113.1");
        _context.RefreshTokens.Add(entity);
        _context.SaveChanges();
        return raw;
    }

    [Fact]
    public async Task Handle_WithValidToken_RotatesToken_AndReturnsNewPair()
    {
        var raw = SeedActiveRefreshToken();
        var handler = CreateHandler();

        var result = await handler.Handle(new RefreshTokenCommand(raw, "203.0.113.2", "xunit-agent"), CancellationToken.None);

        result.RefreshToken.Should().NotBe(raw);

        var oldTokenHash = _tokenService.HashRefreshToken(raw);
        var oldToken = await _context.RefreshTokens.FirstAsync(t => t.TokenHash == oldTokenHash);
        oldToken.IsRevoked.Should().BeTrue();
        oldToken.ReplacedByTokenId.Should().NotBeNull();

        _auditLogger.Entries.Should().ContainSingle(e => e.Action == Domain.Enums.AuditAction.TokenRefresh);
    }

    [Fact]
    public async Task Handle_WithUnknownToken_ThrowsInvalidRefreshTokenException()
    {
        var handler = CreateHandler();

        var act = async () => await handler.Handle(new RefreshTokenCommand("never-issued", null, null), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task Handle_WhenRevokedTokenIsReplayed_RevokesEntireFamily_AndRecordsTheftAudit()
    {
        var raw = SeedActiveRefreshToken();
        var handler = CreateHandler();

        // First use rotates it (valid); second use replays the now-revoked token.
        await handler.Handle(new RefreshTokenCommand(raw, null, null), CancellationToken.None);

        var act = async () => await handler.Handle(new RefreshTokenCommand(raw, "203.0.113.99", "xunit-agent"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
        _auditLogger.Entries.Should().ContainSingle(e => e.Action == Domain.Enums.AuditAction.TokenReuseDetected);

        var allTokensForUser = await _context.RefreshTokens.Where(t => t.UserId == _user.Id).ToListAsync();
        allTokensForUser.Should().OnlyContain(t => t.IsRevoked);
    }

    public void Dispose() => _context.Dispose();
}
