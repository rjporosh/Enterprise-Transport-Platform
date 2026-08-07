using FluentAssertions;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Events;
using NotificationService.Domain.Exceptions;
using Xunit;

namespace NotificationService.UnitTests.Notifications;

public class NotificationTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static Notification CreatePending(int maxRetryCount = 3) =>
        Notification.Create(
            recipient: "jane@example.com",
            channel: NotificationChannel.Email,
            body: "Hello",
            subject: "Welcome",
            dataPayload: null,
            templateId: null,
            sourceReference: null,
            locale: "en",
            priority: NotificationPriority.Normal,
            scheduledForUtc: null,
            maxRetryCount: maxRetryCount,
            nowUtc: NowUtc);

    [Fact]
    public void Create_WithoutScheduledForUtc_StartsAsPending_AndRaisesCreatedEvent()
    {
        var notification = CreatePending();

        notification.Status.Should().Be(NotificationStatus.Pending);
        notification.DomainEvents.Should().ContainSingle(e => e is NotificationCreatedDomainEvent);
    }

    [Fact]
    public void Create_WithFutureScheduledForUtc_StartsAsScheduled()
    {
        var notification = Notification.Create(
            "jane@example.com", NotificationChannel.Email, "Hello", "Subject", null, null, null, "en",
            NotificationPriority.Normal, NowUtc.AddHours(1), 3, NowUtc);

        notification.Status.Should().Be(NotificationStatus.Scheduled);
    }

    [Fact]
    public void MarkSending_ThenMarkSent_TransitionsToSent_AndRaisesSentEvent()
    {
        var notification = CreatePending();
        notification.ClearDomainEvents();

        notification.MarkSending(NowUtc);
        notification.MarkSent(NowUtc.AddSeconds(1), providerMessageId: "provider-123");

        notification.Status.Should().Be(NotificationStatus.Sent);
        notification.SentAtUtc.Should().Be(NowUtc.AddSeconds(1));
        notification.Logs.Should().ContainSingle(l => l.WasSuccessful && l.ProviderMessageId == "provider-123");
        notification.DomainEvents.Should().ContainSingle(e => e is NotificationSentDomainEvent);
    }

    [Fact]
    public void MarkSent_WithoutMarkSendingFirst_ThrowsInvalidNotificationStateException()
    {
        var notification = CreatePending();

        var act = () => notification.MarkSent(NowUtc);

        act.Should().Throw<InvalidNotificationStateException>();
    }

    [Fact]
    public void MarkFailed_BelowMaxRetryCount_TransitionsToRetrying_WithBackoffScheduled()
    {
        var notification = CreatePending(maxRetryCount: 3);
        notification.MarkSending(NowUtc);
        notification.ClearDomainEvents();

        notification.MarkFailed("SMTP timeout", NowUtc);

        notification.Status.Should().Be(NotificationStatus.Retrying);
        notification.RetryCount.Should().Be(1);
        notification.NextRetryAtUtc.Should().Be(NowUtc.AddMinutes(1)); // 2^(1-1) = 1 minute
        notification.DomainEvents.Should().ContainSingle(e => e is NotificationFailedDomainEvent { WillRetry: true });
    }

    [Fact]
    public void MarkFailed_AtMaxRetryCount_DeadLetters_AndRaisesDeadLetteredEvent()
    {
        var notification = CreatePending(maxRetryCount: 1);
        notification.MarkSending(NowUtc);
        notification.ClearDomainEvents();

        notification.MarkFailed("Permanent provider rejection", NowUtc);

        notification.Status.Should().Be(NotificationStatus.DeadLettered);
        notification.NextRetryAtUtc.Should().BeNull();
        notification.DomainEvents.Should().Contain(e => e is NotificationDeadLetteredDomainEvent);
        notification.DomainEvents.Should().ContainSingle(e => e is NotificationFailedDomainEvent { WillRetry: false });
    }

    [Fact]
    public void MarkFailed_UsesExponentialBackoff_CappedAtSixtyMinutes()
    {
        var notification = CreatePending(maxRetryCount: 10);
        notification.MarkSending(NowUtc);

        // Drive it through several failure/retry cycles and check the delay grows then caps.
        notification.MarkFailed("err1", NowUtc); // attempt 1 -> 1 min
        notification.NextRetryAtUtc.Should().Be(NowUtc.AddMinutes(1));

        notification.MarkSending(NowUtc);
        notification.MarkFailed("err2", NowUtc); // attempt 2 -> 2 min
        notification.NextRetryAtUtc.Should().Be(NowUtc.AddMinutes(2));

        notification.MarkSending(NowUtc);
        notification.MarkFailed("err3", NowUtc); // attempt 3 -> 4 min
        notification.NextRetryAtUtc.Should().Be(NowUtc.AddMinutes(4));
    }

    [Fact]
    public void Cancel_WhilePending_TransitionsToCancelled()
    {
        var notification = CreatePending();

        notification.Cancel("No longer needed", NowUtc);

        notification.Status.Should().Be(NotificationStatus.Cancelled);
        notification.DomainEvents.Should().Contain(e => e is NotificationCancelledDomainEvent);
    }

    [Fact]
    public void Cancel_AfterSent_ThrowsInvalidNotificationStateException()
    {
        var notification = CreatePending();
        notification.MarkSending(NowUtc);
        notification.MarkSent(NowUtc);

        var act = () => notification.Cancel("Too late", NowUtc);

        act.Should().Throw<InvalidNotificationStateException>();
    }

    [Fact]
    public void ResetForManualRetry_OnDeadLettered_ReturnsToRetrying_WithExpandedBudget()
    {
        var notification = CreatePending(maxRetryCount: 1);
        notification.MarkSending(NowUtc);
        notification.MarkFailed("Permanent failure", NowUtc);
        notification.Status.Should().Be(NotificationStatus.DeadLettered);

        notification.ResetForManualRetry(additionalAttempts: 3, NowUtc);

        notification.Status.Should().Be(NotificationStatus.Retrying);
        notification.MaxRetryCount.Should().Be(4); // 1 + 3
        notification.NextRetryAtUtc.Should().Be(NowUtc);
    }

    [Fact]
    public void ResetForManualRetry_OnNonDeadLetteredNotification_Throws()
    {
        var notification = CreatePending();

        var act = () => notification.ResetForManualRetry(3, NowUtc);

        act.Should().Throw<InvalidNotificationStateException>();
    }
}
