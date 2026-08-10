using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Events;

public sealed record NotificationCreatedDomainEvent(
    Guid NotificationId,
    NotificationChannel Channel,
    string Recipient,
    NotificationPriority Priority) : Common.DomainEvent;
