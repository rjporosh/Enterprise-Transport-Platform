namespace NotificationService.Domain.Events;

public sealed record NotificationCancelledDomainEvent(Guid NotificationId, string Reason) : Common.DomainEvent;
