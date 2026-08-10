namespace NotificationService.Domain.Events;

/// <summary>Raised once MaxRetryCount is exhausted. Consumed by an alerting/on-call integration (not part of this service) so a human can inspect and manually resolve.</summary>
public sealed record NotificationDeadLetteredDomainEvent(Guid NotificationId, string LastError, int TotalAttempts) : Common.DomainEvent;
