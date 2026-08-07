namespace NotificationService.Domain.Enums;

/// <summary>
/// Lifecycle of a single notification. Linear, with one loop-back edge:
/// Pending/Scheduled -&gt; Sending -&gt; Sent | Failed, and Failed -&gt; Retrying -&gt; Sending
/// again until MaxRetryCount is exhausted, at which point it becomes
/// DeadLettered and requires manual intervention (see RetryNotification).
/// </summary>
public enum NotificationStatus
{
    Pending = 1,
    Scheduled = 2,
    Sending = 3,
    Sent = 4,
    Delivered = 5,
    Failed = 6,
    Retrying = 7,
    DeadLettered = 8,
    Cancelled = 9
}
