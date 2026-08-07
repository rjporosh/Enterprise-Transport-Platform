using NotificationService.Domain.Common;

namespace NotificationService.Domain.Entities;

/// <summary>
/// Immutable record of a single delivery attempt for a Notification.
/// Child entity — never loaded/queried on its own outside of its parent's
/// Logs collection, which is why it has no public factory beyond the two
/// intention-revealing helpers used by Notification itself.
/// </summary>
public sealed class NotificationLog : Entity
{
    public Guid NotificationId { get; private set; }
    public int AttemptNumber { get; private set; }
    public bool WasSuccessful { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public string? Error { get; private set; }
    public DateTimeOffset AttemptedAtUtc { get; private set; }

    private NotificationLog() { } // EF Core

    private NotificationLog(Guid notificationId, int attemptNumber, bool wasSuccessful,
        DateTimeOffset attemptedAtUtc, string? providerMessageId, string? error) : base(Guid.NewGuid())
    {
        NotificationId = notificationId;
        AttemptNumber = attemptNumber;
        WasSuccessful = wasSuccessful;
        AttemptedAtUtc = attemptedAtUtc;
        ProviderMessageId = providerMessageId;
        Error = error;
    }

    public static NotificationLog Success(Guid notificationId, int attemptNumber, DateTimeOffset nowUtc, string? providerMessageId) =>
        new(notificationId, attemptNumber, wasSuccessful: true, nowUtc, providerMessageId, error: null);

    public static NotificationLog Failure(Guid notificationId, int attemptNumber, DateTimeOffset nowUtc, string error) =>
        new(notificationId, attemptNumber, wasSuccessful: false, nowUtc, providerMessageId: null, error);
}
