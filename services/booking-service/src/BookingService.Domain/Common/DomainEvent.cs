using MediatR;

namespace BookingService.Domain.Common;

/// <summary>
/// Marker base for domain events. Implements INotification so the same event
/// can be dispatched in-process via MediatR AND persisted to the transactional
/// outbox for reliable, at-least-once delivery to RabbitMQ (see
/// docs/events/Outbox.md for the delivery guarantee this buys us).
/// </summary>
public abstract record DomainEvent : INotification
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
