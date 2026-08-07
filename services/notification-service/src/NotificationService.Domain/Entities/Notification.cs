using NotificationService.Domain.Common;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Events;
using NotificationService.Domain.Exceptions;

namespace NotificationService.Domain.Entities;

/// <summary>
/// A single outbound message on one channel (Email/SMS/Push) to one recipient.
/// This is the aggregate root for the whole bounded context: NotificationLog
/// entries (delivery attempts) are children loaded/saved only through it.
///
/// State machine:
///   Pending/Scheduled -> Sending -> Sent -> Delivered (optional, provider-dependent)
///                                \-> Failed -> Retrying -> Sending (loop, up to MaxRetryCount)
///                                           \-> DeadLettered (retries exhausted)
///   Pending/Scheduled -> Cancelled (only before Sending starts)
/// </summary>
public sealed class Notification : AggregateRoot
{
    public const int DefaultMaxRetryCount = 5;

    private readonly List<NotificationLog> _logs = new();

    public string Recipient { get; private set; } = default!;
    public NotificationChannel Channel { get; private set; }
    public NotificationStatus Status { get; private set; }
    public NotificationPriority Priority { get; private set; }

    public string? Subject { get; private set; }
    public string Body { get; private set; } = default!;
    /// <summary>Provider-specific structured payload (e.g. FCM "data" map, serialized JSON). Null for Email/Sms.</summary>
    public string? DataPayload { get; private set; }

    public Guid? TemplateId { get; private set; }
    /// <summary>Correlates this notification back to the business event/aggregate that triggered it (e.g. a BookingId), for support/audit lookups. Free-form because the trigger can come from any upstream service.</summary>
    public string? SourceReference { get; private set; }
    public string? Locale { get; private set; }

    public DateTimeOffset? ScheduledForUtc { get; private set; }
    public DateTimeOffset? SentAtUtc { get; private set; }
    public DateTimeOffset? DeliveredAtUtc { get; private set; }

    public int RetryCount { get; private set; }
    public int MaxRetryCount { get; private set; } = DefaultMaxRetryCount;
    public DateTimeOffset? NextRetryAtUtc { get; private set; }
    public string? LastError { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<NotificationLog> Logs => _logs.AsReadOnly();

    private Notification() { } // EF Core

    private Notification(
        Guid id,
        string recipient,
        NotificationChannel channel,
        string body,
        string? subject,
        string? dataPayload,
        Guid? templateId,
        string? sourceReference,
        string? locale,
        NotificationPriority priority,
        DateTimeOffset? scheduledForUtc,
        int maxRetryCount,
        DateTimeOffset nowUtc) : base(id)
    {
        Recipient = recipient;
        Channel = channel;
        Body = body;
        Subject = subject;
        DataPayload = dataPayload;
        TemplateId = templateId;
        SourceReference = sourceReference;
        Locale = locale;
        Priority = priority;
        ScheduledForUtc = scheduledForUtc;
        MaxRetryCount = maxRetryCount <= 0 ? DefaultMaxRetryCount : maxRetryCount;
        Status = scheduledForUtc.HasValue && scheduledForUtc.Value > nowUtc
            ? NotificationStatus.Scheduled
            : NotificationStatus.Pending;
        CreatedAtUtc = nowUtc;

        Raise(new NotificationCreatedDomainEvent(Id, Channel, Recipient, Priority));
    }

    public static Notification Create(
        string recipient,
        NotificationChannel channel,
        string body,
        string? subject,
        string? dataPayload,
        Guid? templateId,
        string? sourceReference,
        string? locale,
        NotificationPriority priority,
        DateTimeOffset? scheduledForUtc,
        int maxRetryCount,
        DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(recipient))
            throw new ArgumentException("Recipient is required.", nameof(recipient));
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Body is required.", nameof(body));

        return new Notification(
            Guid.NewGuid(), recipient, channel, body, subject, dataPayload,
            templateId, sourceReference, locale, priority, scheduledForUtc, maxRetryCount, nowUtc);
    }

    /// <summary>Transitions Pending/Scheduled/Retrying -&gt; Sending. Called by the dispatch job immediately before invoking the channel provider, so a crash mid-send is visible as a stuck "Sending" row rather than a silently-lost "Pending" one.</summary>
    public void MarkSending(DateTimeOffset nowUtc)
    {
        if (Status is not (NotificationStatus.Pending or NotificationStatus.Scheduled or NotificationStatus.Retrying))
            throw new InvalidNotificationStateException(
                $"Cannot mark notification '{Id}' as Sending from status '{Status}'.");

        Status = NotificationStatus.Sending;
        UpdatedAtUtc = nowUtc;
    }

    public void MarkSent(DateTimeOffset nowUtc, string? providerMessageId = null)
    {
        if (Status != NotificationStatus.Sending)
            throw new InvalidNotificationStateException(
                $"Cannot mark notification '{Id}' as Sent from status '{Status}'.");

        Status = NotificationStatus.Sent;
        SentAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
        NextRetryAtUtc = null;

        _logs.Add(NotificationLog.Success(Id, RetryCount + 1, nowUtc, providerMessageId));
        Raise(new NotificationSentDomainEvent(Id, Channel, Recipient, nowUtc));
    }

    public void MarkDelivered(DateTimeOffset nowUtc)
    {
        if (Status != NotificationStatus.Sent)
            throw new InvalidNotificationStateException(
                $"Cannot mark notification '{Id}' as Delivered from status '{Status}'.");

        Status = NotificationStatus.Delivered;
        DeliveredAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;

        Raise(new NotificationDeliveredDomainEvent(Id, Channel, nowUtc));
    }

    /// <summary>
    /// Records a failed send attempt. If retries remain, schedules the next
    /// attempt using exponential backoff with a cap (see NextRetryDelay);
    /// otherwise dead-letters the notification for manual triage.
    /// </summary>
    public void MarkFailed(string error, DateTimeOffset nowUtc)
    {
        if (Status is not (NotificationStatus.Sending or NotificationStatus.Retrying))
            throw new InvalidNotificationStateException(
                $"Cannot mark notification '{Id}' as Failed from status '{Status}'.");

        RetryCount++;
        LastError = Truncate(error, 4000);
        UpdatedAtUtc = nowUtc;
        _logs.Add(NotificationLog.Failure(Id, RetryCount, nowUtc, LastError));

        var willRetry = RetryCount < MaxRetryCount;
        if (willRetry)
        {
            Status = NotificationStatus.Retrying;
            NextRetryAtUtc = nowUtc.Add(NextRetryDelay(RetryCount));
        }
        else
        {
            Status = NotificationStatus.DeadLettered;
            NextRetryAtUtc = null;
            Raise(new NotificationDeadLetteredDomainEvent(Id, LastError, RetryCount));
        }

        Raise(new NotificationFailedDomainEvent(Id, Channel, Recipient, LastError, RetryCount, willRetry));
    }

    /// <summary>Resets a DeadLettered notification back into the retry loop with a fresh budget — used by the manual "retry" API/operator action, distinct from the automatic Failed-&gt;Retrying transition.</summary>
    public void ResetForManualRetry(int additionalAttempts, DateTimeOffset nowUtc)
    {
        if (Status != NotificationStatus.DeadLettered)
            throw new InvalidNotificationStateException(
                $"Only DeadLettered notifications can be manually retried (current status: '{Status}').");

        MaxRetryCount += Math.Max(1, additionalAttempts);
        Status = NotificationStatus.Retrying;
        NextRetryAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public void Cancel(string reason, DateTimeOffset nowUtc)
    {
        if (Status is not (NotificationStatus.Pending or NotificationStatus.Scheduled or NotificationStatus.Retrying))
            throw new InvalidNotificationStateException(
                $"Cannot cancel notification '{Id}' from status '{Status}'.");

        Status = NotificationStatus.Cancelled;
        UpdatedAtUtc = nowUtc;
        NextRetryAtUtc = null;

        Raise(new NotificationCancelledDomainEvent(Id, reason));
    }

    public void SoftDelete(DateTimeOffset nowUtc)
    {
        IsDeleted = true;
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>Exponential backoff with a 1-hour cap: 1m, 2m, 4m, 8m, 16m, ... capped. Deliberately no jitter here — jitter for the retry *poll* itself is applied by the Quartz job's own scan cadence, so adding jitter to the stored NextRetryAtUtc as well would just make support queries ("when will this retry?") harder to reason about for no benefit at this scale.</summary>
    private static TimeSpan NextRetryDelay(int attemptNumber)
    {
        var minutes = Math.Min(60, Math.Pow(2, attemptNumber - 1));
        return TimeSpan.FromMinutes(minutes);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
