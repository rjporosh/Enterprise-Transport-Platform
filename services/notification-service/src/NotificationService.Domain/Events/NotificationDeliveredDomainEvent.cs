using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Events;

/// <summary>Raised when a provider delivery receipt/webhook confirms end-user delivery (e.g. SMS carrier DLR, push token ack). Optional — not every channel/provider supports this; notifications that never receive one simply remain in Sent status, which is still a successful outcome.</summary>
public sealed record NotificationDeliveredDomainEvent(
    Guid NotificationId,
    NotificationChannel Channel,
    DateTimeOffset DeliveredAtUtc) : Common.DomainEvent;
