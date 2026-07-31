using BookingService.Domain.Common;

namespace BookingService.Domain.Events;

/// <summary>Raised once payment succeeds and seats are permanently allocated.</summary>
public sealed record BookingConfirmedDomainEvent(
    Guid BookingId,
    Guid TripId,
    Guid CustomerId) : DomainEvent;
