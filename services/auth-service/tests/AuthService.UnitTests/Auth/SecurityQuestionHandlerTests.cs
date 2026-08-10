using AuthService.Application.Features.Auth.SecurityQuestions;
using AuthService.Domain.Enums;
using AuthService.Domain.Entities;
using AuthService.Domain.Exceptions;
using AuthService.Infrastructure.Services;
using AuthService.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AuthService.UnitTests.Auth;

public class SecurityQuestionHandlerTests : IDisposable
{
    private readonly TestAuthDbContext _context;
    private readonly FakeAuditLogger _auditLogger = new();
    private readonly FakeDateTimeProvider _clock = new();
    private ConfigureSecurityQuestionsHandler _configureHandler = default!;
    private VerifySecurityQuestionsHandler _verifyHandler = default!;
    private User _user = default!;
    private Guid _questionId1;
    private Guid _questionId2;
    private Guid _questionId3;

    public SecurityQuestionHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestAuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestAuthDbContext(options);
        _questionId1 = Guid.NewGuid();
        _questionId2 = Guid.NewGuid();
        _questionId3 = Guid.NewGuid();
        Setup();
    }

    private void Setup()
    {
        _context.SecurityQuestions.Add(new AuthService.Domain.Entities.SecurityQuestion(_questionId1, "What is your pet name?"));
        _context.SecurityQuestions.Add(new AuthService.Domain.Entities.SecurityQuestion(_questionId2, "What is your birth city?"));
        _context.SecurityQuestions.Add(new AuthService.Domain.Entities.SecurityQuestion(_questionId3, "What is your favorite food?"));
        _context.SaveChanges();

        _configureHandler = new ConfigureSecurityQuestionsHandler(_context, _auditLogger, NullLogger<ConfigureSecurityQuestionsHandler>.Instance);
        _verifyHandler = new VerifySecurityQuestionsHandler(new SecurityAnswerValidator(_context, NullLogger<SecurityAnswerValidator>.Instance), _context, _auditLogger, NullLogger<VerifySecurityQuestionsHandler>.Instance);

        _user = User.Register(Guid.NewGuid(), "sq@example.com", "hash", "Security", "User", null, _clock.UtcNow);
        _context.Users.Add(_user);
        _context.SaveChanges();
    }

    [Fact]
    public async Task ConfigureSecurityQuestions_WithThreeQuestions_ConfiguresSuccessfully()
    {
        var answers = new Dictionary<Guid, string>
        {
            [_questionId1] = "Fluffy",
            [_questionId2] = "Dhaka",
            [_questionId3] = "Pizza"
        };

        await _configureHandler.Handle(new ConfigureSecurityQuestionsCommand(_user.Id, answers, null, null), CancellationToken.None);
        _auditLogger.Entries.Should().ContainSingle(e => e.Action == AuditAction.SecurityQuestionConfigured);

        var storedAnswers = await _context.SecurityAnswers.Where(sa => sa.UserId == _user.Id).ToListAsync();
        storedAnswers.Should().HaveCount(3);
    }

    [Fact]
    public async Task VerifySecurityQuestions_WithCorrectAnswers_VerifiesSuccessfully()
    {
        var answers = new Dictionary<Guid, string>
        {
            [_questionId1] = "Fluffy",
            [_questionId2] = "Dhaka",
            [_questionId3] = "Pizza"
        };
        await _configureHandler.Handle(new ConfigureSecurityQuestionsCommand(_user.Id, answers, null, null), CancellationToken.None);

        await _verifyHandler.Handle(new VerifySecurityQuestionsCommand(_user.Id, answers, null, null), CancellationToken.None);
        _auditLogger.Entries.Should().Contain(e => e.Action == AuditAction.SecurityQuestionVerified);
    }

    [Fact]
    public async Task VerifySecurityQuestions_AnswerIsCaseInsensitive()
    {
        var answers = new Dictionary<Guid, string>
        {
            [_questionId1] = "Fluffy",
            [_questionId2] = "Dhaka",
            [_questionId3] = "Pizza"
        };
        await _configureHandler.Handle(new ConfigureSecurityQuestionsCommand(_user.Id, answers, null, null), CancellationToken.None);

        var differentCase = new Dictionary<Guid, string>
        {
            [_questionId1] = "FLUFFY",
            [_questionId2] = "dhaka",
            [_questionId3] = "pizza"
        };
        await _verifyHandler.Handle(new VerifySecurityQuestionsCommand(_user.Id, differentCase, null, null), CancellationToken.None);
        _auditLogger.Entries.Should().Contain(e => e.Action == AuditAction.SecurityQuestionVerified);
    }

    [Fact]
    public async Task VerifySecurityQuestions_AnswerWithWhitespace_IsNormalized()
    {
        var answers = new Dictionary<Guid, string>
        {
            [_questionId1] = "Fluffy",
            [_questionId2] = "Dhaka",
            [_questionId3] = "Pizza"
        };
        await _configureHandler.Handle(new ConfigureSecurityQuestionsCommand(_user.Id, answers, null, null), CancellationToken.None);

        var withWhitespace = new Dictionary<Guid, string>
        {
            [_questionId1] = "  Fluffy  ",
            [_questionId2] = " Dhaka ",
            [_questionId3] = "Pizza "
        };
        await _verifyHandler.Handle(new VerifySecurityQuestionsCommand(_user.Id, withWhitespace, null, null), CancellationToken.None);
        _auditLogger.Entries.Should().Contain(e => e.Action == AuditAction.SecurityQuestionVerified);
    }

    [Fact]
    public async Task VerifySecurityQuestions_WithWrongAnswer_ThrowsInvalidSecurityAnswerException()
    {
        var answers = new Dictionary<Guid, string>
        {
            [_questionId1] = "Fluffy",
            [_questionId2] = "Dhaka",
            [_questionId3] = "Pizza"
        };
        await _configureHandler.Handle(new ConfigureSecurityQuestionsCommand(_user.Id, answers, null, null), CancellationToken.None);

        var wrongAnswers = new Dictionary<Guid, string>
        {
            [_questionId1] = "Wrong1",
            [_questionId2] = "Dhaka",
            [_questionId3] = "Pizza"
        };
        var act = async () => await _verifyHandler.Handle(new VerifySecurityQuestionsCommand(_user.Id, wrongAnswers, null, null), CancellationToken.None);
        await act.Should().ThrowAsync<AuthService.Domain.Exceptions.InvalidSecurityAnswerException>();
    }

    public void Dispose() => _context.Dispose();
}
