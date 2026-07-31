using BookingService.Domain.Common;

namespace BookingService.Domain.Events;

/// <summary>
/// Raised when a booking is created in PendingPayment state. Consumed by the
/// Payment Service (to start a payment intent) and Notification Service
/// (to send a "booking held" confirmation) over RabbitMQ via the outbox.
/// </summary>
public sealed record BookingCreatedDomainEvent(
    Guid BookingId,
    Guid TripId,
    Guid CustomerId,
    decimal TotalAmount,
    string Currency,
    IReadOnlyCollection<string> SeatNumbers) : DomainEvent;
