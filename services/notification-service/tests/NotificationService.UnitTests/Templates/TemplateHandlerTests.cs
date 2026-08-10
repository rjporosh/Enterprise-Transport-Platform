using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Features.Templates.CreateTemplate;
using NotificationService.Application.Features.Templates.UpdateTemplate;
using NotificationService.Domain.Enums;
using NotificationService.UnitTests.TestSupport;
using Xunit;

namespace NotificationService.UnitTests.Templates;

public class TemplateHandlerTests : IDisposable
{
    private readonly TestNotificationDbContext _context;
    private readonly FakeDateTimeProvider _clock = new();

    public TemplateHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestNotificationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestNotificationDbContext(options);
    }

    private static CreateTemplateCommand ValidCreateCommand(string key = "booking.confirmed") =>
        new(key, TemplateChannel.Email, "en", "Booking confirmed", "Sent after payment succeeds",
            "Your booking is confirmed", "Hi {{firstName}}, your booking is confirmed.", null);

    [Fact]
    public async Task Create_WithNewKey_Succeeds()
    {
        var handler = new CreateTemplateHandler(_context, _clock);

        var result = await handler.Handle(ValidCreateCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _context.NotificationTemplates.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Create_WithDuplicateKeyChannelLocale_ReturnsConflict()
    {
        var handler = new CreateTemplateHandler(_context, _clock);
        await handler.Handle(ValidCreateCommand(), CancellationToken.None);

        var result = await handler.Handle(ValidCreateCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "CONFLICT");
    }

    [Fact]
    public async Task Create_NormalizesKeyAndLocale_ToLowercase()
    {
        var handler = new CreateTemplateHandler(_context, _clock);

        var result = await handler.Handle(ValidCreateCommand("Booking.Confirmed") with { Locale = "EN" }, CancellationToken.None);

        result.Value!.Key.Should().Be("booking.confirmed");
        result.Value.Locale.Should().Be("en");
    }

    [Fact]
    public async Task Update_OnExistingTemplate_IncrementsVersion_AndAppliesChanges()
    {
        var createHandler = new CreateTemplateHandler(_context, _clock);
        var created = await createHandler.Handle(ValidCreateCommand(), CancellationToken.None);
        var updateHandler = new UpdateTemplateHandler(_context, _clock);

        var result = await updateHandler.Handle(
            new UpdateTemplateCommand(created.Value!.Id, "New name", "New description", "New subject", "New body", null, IsActive: false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Version.Should().Be(2);
        result.Value.IsActive.Should().BeFalse();
        result.Value.Body.Should().Be("New body");
    }

    [Fact]
    public async Task Update_OnUnknownId_ReturnsNotFound()
    {
        var handler = new UpdateTemplateHandler(_context, _clock);

        var result = await handler.Handle(
            new UpdateTemplateCommand(Guid.NewGuid(), "name", null, "subject", "body", null, true),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "NOT_FOUND");
    }

    public void Dispose() => _context.Dispose();
}
