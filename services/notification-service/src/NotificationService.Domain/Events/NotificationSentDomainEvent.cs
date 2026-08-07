using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Events;

/// <summary>Raised the instant a channel provider accepts the message (not necessarily "delivered to the handset/inbox" — see NotificationDeliveredDomainEvent for provider delivery-receipt callbacks, where supported).</summary>
public sealed record NotificationSentDomainEvent(
    Guid NotificationId,
    NotificationChannel Channel,
    string Recipient,
    DateTimeOffset SentAtUtc) : Common.DomainEvent;
