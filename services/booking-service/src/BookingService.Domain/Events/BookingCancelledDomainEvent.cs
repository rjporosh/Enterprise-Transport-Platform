using BookingService.Domain.Common;

namespace BookingService.Domain.Events;

/// <summary>Raised when a booking is cancelled, releasing its seats back to the trip.</summary>
public sealed record BookingCancelledDomainEvent(
    Guid BookingId,
    Guid TripId,
    string Reason) : DomainEvent;
