using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Features.Notifications.SendNotification;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.UnitTests.TestSupport;
using Xunit;

namespace NotificationService.UnitTests.Notifications;

public class SendNotificationHandlerTests : IDisposable
{
    private readonly TestNotificationDbContext _context;
    private readonly FakeEventPublisher _eventPublisher = new();
    private readonly FakeDateTimeProvider _clock = new();
    private readonly FakeTemplateRenderer _templateRenderer = new();

    public SendNotificationHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestNotificationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestNotificationDbContext(options);
    }

    private SendNotificationHandler CreateHandler() => new(_context, _eventPublisher, _clock, _templateRenderer);

    private static SendNotificationCommand BasicCommand(
        string? templateKey = null, IReadOnlyDictionary<string, object?>? variables = null,
        string? body = "Hello there", string? recipientId = null, bool isTransactional = true,
        DateTimeOffset? scheduledForUtc = null) =>
        new(
            Recipient: "jane@example.com",
            Channel: NotificationChannel.Email,
            TemplateKey: templateKey,
            TemplateVariables: variables,
            Subject: templateKey is null ? "Subject" : null,
            Body: templateKey is null ? body : null,
            DataPayload: null,
            RecipientId: recipientId,
            SourceReference: null,
            Locale: null,
            Priority: NotificationPriority.Normal,
            ScheduledForUtc: scheduledForUtc,
            MaxRetryCount: null,
            IsTransactional: isTransactional);

    [Fact]
    public async Task Handle_WithExplicitBody_CreatesPendingNotification_AndPublishesCreatedEvent()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(BasicCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(NotificationStatus.Pending);
        (await _context.Notifications.CountAsync()).Should().Be(1);
        _eventPublisher.PublishedEvents.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_WithFutureScheduledForUtc_CreatesScheduledNotification()
    {
        var handler = CreateHandler();
        _clock.UtcNow = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var scheduledFor = _clock.UtcNow.AddDays(1);

        var result = await handler.Handle(BasicCommand(scheduledForUtc: scheduledFor), CancellationToken.None);

        result.Value!.Status.Should().Be(NotificationStatus.Scheduled);
        result.Value.ScheduledForUtc.Should().Be(scheduledFor);
    }

    [Fact]
    public async Task Handle_WithTemplateKey_RendersSubjectAndBody_FromMatchingActiveTemplate()
    {
        var template = NotificationTemplate.Create(
            "booking.confirmed", TemplateChannel.Email, "en", "Booking confirmed",
            null, "Booking {{bookingId}} confirmed", "Hi {{firstName}}, your booking {{bookingId}} is confirmed.",
            null, _clock.UtcNow);
        _context.NotificationTemplates.Add(template);
        await _context.SaveChangesAsync();

        var handler = CreateHandler();
        var variables = new Dictionary<string, object?> { ["firstName"] = "Jane", ["bookingId"] = "B-1" };

        var result = await handler.Handle(BasicCommand(templateKey: "booking.confirmed", variables: variables), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = await _context.Notifications.FirstAsync();
        saved.Subject.Should().Be("Booking B-1 confirmed");
        saved.Body.Should().Be("Hi Jane, your booking B-1 is confirmed.");
        saved.TemplateId.Should().Be(template.Id);
    }

    [Fact]
    public async Task Handle_WithUnknownTemplateKey_ReturnsNotFoundError()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(BasicCommand(templateKey: "does.not.exist"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "NOT_FOUND");
    }

    [Fact]
    public async Task Handle_NonTransactional_ForOptedOutRecipient_ReturnsConflictError_AndDoesNotCreateNotification()
    {
        var preference = RecipientPreference.CreateDefault("user-1", "en", _clock.UtcNow);
        preference.UpdatePreferences(emailOptOut: true, smsOptOut: false, pushOptOut: false, "en", _clock.UtcNow);
        _context.RecipientPreferences.Add(preference);
        await _context.SaveChangesAsync();

        var handler = CreateHandler();

        var result = await handler.Handle(BasicCommand(recipientId: "user-1", isTransactional: false), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "CONFLICT");
        (await _context.Notifications.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_Transactional_ForOptedOutRecipient_StillSends()
    {
        var preference = RecipientPreference.CreateDefault("user-1", "en", _clock.UtcNow);
        preference.UpdatePreferences(emailOptOut: true, smsOptOut: false, pushOptOut: false, "en", _clock.UtcNow);
        _context.RecipientPreferences.Add(preference);
        await _context.SaveChangesAsync();

        var handler = CreateHandler();

        var result = await handler.Handle(BasicCommand(recipientId: "user-1", isTransactional: true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    // ---- Inbox dedup tests (M7) -------------------------------------------------

    [Fact]
    public async Task Handle_WithDuplicateSourceReference_ReturnsDedupSuccess_WithoutCreatingSecondRow()
    {
        // Simulate RabbitMQ at-least-once redelivery: the same event id arrives twice.
        var sourceRef = $"ticket.issued:{Guid.NewGuid()}";
        var command = new SendNotificationCommand(
            Recipient: "alice@example.com",
            Channel: NotificationChannel.Email,
            TemplateKey: null,
            TemplateVariables: null,
            Subject: "Ticket ready",
            Body: "Your ticket is ready.",
            DataPayload: null,
            RecipientId: null,
            SourceReference: sourceRef,
            Locale: null,
            Priority: NotificationPriority.Normal,
            ScheduledForUtc: null,
            MaxRetryCount: null,
            IsTransactional: true);

        var handler = CreateHandler();

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None); // same SourceReference

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        // Only one row — the second call was suppressed.
        (await _context.Notifications.CountAsync()).Should().Be(1);
        // The dedup hit returns a phantom dto with Guid.Empty so the consumer
        // can distinguish a real creation from a suppressed duplicate.
        second.Value!.NotificationId.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task Handle_WithNullSourceReference_AlwaysCreatesNewRow()
    {
        // Two notifications with no SourceReference must both be created —
        // null means "not an event-driven send, no dedup" (REST callers
        // may legitimately send the same body twice without a SourceReference).
        var handler = CreateHandler();

        var first = await handler.Handle(BasicCommand(), CancellationToken.None);
        var second = await handler.Handle(BasicCommand(), CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        (await _context.Notifications.CountAsync()).Should().Be(2);
    }

    public void Dispose() => _context.Dispose();
}
