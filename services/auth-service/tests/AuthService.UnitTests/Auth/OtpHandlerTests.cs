using AuthService.Application.Common.Interfaces;
using AuthService.Application.Features.Auth.Otp;
using AuthService.Domain.Enums;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Entities;
using AuthService.Infrastructure.Services;
using AuthService.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AuthService.UnitTests.Auth;

public class OtpHandlerTests : IDisposable
{
    private readonly TestAuthDbContext _context;
    private readonly FakeEventPublisher _eventPublisher = new();
    private readonly FakeDateTimeProvider _clock = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeTokenService _tokenService = new();
    private readonly FakeAuditLogger _auditLogger = new();
    private readonly FakeEmailSender _emailSender = new();
    private readonly FakeSmsSender _smsSender = new();
    private OtpService _otpService = default!;
    private RequestOtpHandler _requestHandler = default!;
    private VerifyOtpHandler _verifyHandler = default!;
    private User _user = default!;

    public OtpHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestAuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestAuthDbContext(options);
        Setup();
    }

    private void Setup()
    {
        _otpService = new OtpService(_context, _emailSender, _smsSender, _clock, NullLogger<OtpService>.Instance);
        _requestHandler = new RequestOtpHandler(_context, _otpService, _auditLogger, NullLogger<RequestOtpHandler>.Instance);
        _verifyHandler = new VerifyOtpHandler(_context, _otpService, _auditLogger, NullLogger<VerifyOtpHandler>.Instance);

        _user = User.Register(Guid.NewGuid(), "otp@example.com", _passwordHasher.Hash("password"), "Otp", "User", null, _clock.UtcNow);
        _context.Users.Add(_user);
        _context.SaveChanges();
    }

    [Fact]
    public async Task RequestOtp_WithEmail_SendsOtp()
    {
        await _requestHandler.Handle(new RequestOtpCommand(_user.Id, "email", _user.Email, null, null), CancellationToken.None);
        _auditLogger.Entries.Should().ContainSingle(e => e.Action == AuditAction.OtpRequested);
        _emailSender.SentEmails.Should().HaveCount(1);
    }

    [Fact]
    public async Task VerifyOtp_WithInvalidCode_ThrowsInvalidOtpException()
    {
        await _requestHandler.Handle(new RequestOtpCommand(_user.Id, "email", _user.Email, null, null), CancellationToken.None);

        var act = async () => await _verifyHandler.Handle(new VerifyOtpCommand(_user.Id, "000000", "email", null, null), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOtpException>();
    }

    public void Dispose() => _context.Dispose();
}
