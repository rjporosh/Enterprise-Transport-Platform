using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Features.Notifications.CancelNotification;
using NotificationService.Application.Features.Notifications.RetryNotification;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.UnitTests.TestSupport;
using Xunit;

namespace NotificationService.UnitTests.Notifications;

public class CancelAndRetryNotificationHandlerTests : IDisposable
{
    private readonly TestNotificationDbContext _context;
    private readonly FakeEventPublisher _eventPublisher = new();
    private readonly FakeDateTimeProvider _clock = new();

    public CancelAndRetryNotificationHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestNotificationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestNotificationDbContext(options);
    }

    private Notification SeedPendingNotification()
    {
        var notification = Notification.Create(
            "jane@example.com", NotificationChannel.Email, "Hello", "Subject", null, null, null, "en",
            NotificationPriority.Normal, null, 3, _clock.UtcNow);
        notification.ClearDomainEvents();
        _context.Notifications.Add(notification);
        _context.SaveChanges();
        return notification;
    }

    private Notification SeedDeadLetteredNotification()
    {
        var notification = Notification.Create(
            "jane@example.com", NotificationChannel.Email, "Hello", "Subject", null, null, null, "en",
            NotificationPriority.Normal, null, 1, _clock.UtcNow);
        notification.MarkSending(_clock.UtcNow);
        notification.MarkFailed("permanent failure", _clock.UtcNow);
        notification.ClearDomainEvents();
        _context.Notifications.Add(notification);
        _context.SaveChanges();
        return notification;
    }

    [Fact]
    public async Task Cancel_OnPendingNotification_Succeeds()
    {
        var notification = SeedPendingNotification();
        var handler = new CancelNotificationHandler(_context, _eventPublisher, _clock);

        var result = await handler.Handle(new CancelNotificationCommand(notification.Id, "No longer needed"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _context.Notifications.FindAsync(notification.Id))!.Status.Should().Be(NotificationStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_OnUnknownId_ReturnsNotFound()
    {
        var handler = new CancelNotificationHandler(_context, _eventPublisher, _clock);

        var result = await handler.Handle(new CancelNotificationCommand(Guid.NewGuid(), "reason"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "NOT_FOUND");
    }

    [Fact]
    public async Task Cancel_OnAlreadySentNotification_ReturnsInvalidState()
    {
        var notification = SeedPendingNotification();
        notification.MarkSending(_clock.UtcNow);
        notification.MarkSent(_clock.UtcNow);

        var handler = new CancelNotificationHandler(_context, _eventPublisher, _clock);
        var result = await handler.Handle(new CancelNotificationCommand(notification.Id, "too late"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "INVALID_STATE");
    }

    [Fact]
    public async Task Retry_OnDeadLetteredNotification_Succeeds_AndExpandsBudget()
    {
        var notification = SeedDeadLetteredNotification();
        var handler = new RetryNotificationHandler(_context, _clock);

        var result = await handler.Handle(new RetryNotificationCommand(notification.Id, AdditionalAttempts: 2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var reloaded = await _context.Notifications.FindAsync(notification.Id);
        reloaded!.Status.Should().Be(NotificationStatus.Retrying);
        reloaded.MaxRetryCount.Should().Be(3); // 1 + 2
    }

    [Fact]
    public async Task Retry_OnPendingNotification_ReturnsInvalidState()
    {
        var notification = SeedPendingNotification();
        var handler = new RetryNotificationHandler(_context, _clock);

        var result = await handler.Handle(new RetryNotificationCommand(notification.Id, 2), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "INVALID_STATE");
    }

    public void Dispose() => _context.Dispose();
}
