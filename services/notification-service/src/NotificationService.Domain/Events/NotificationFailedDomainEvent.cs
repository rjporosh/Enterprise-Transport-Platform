using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Events;

public sealed record NotificationFailedDomainEvent(
    Guid NotificationId,
    NotificationChannel Channel,
    string Recipient,
    string Reason,
    int AttemptNumber,
    bool WillRetry) : Common.DomainEvent;
