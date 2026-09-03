using BookingService.Domain.Common;

namespace BookingService.Domain.Events;

/// <summary>
/// Raised once payment succeeds and seats are permanently allocated. Carries
/// a full journey + customer snapshot so the Ticketing Service can issue a
/// ticket and the Notification Service can email/SMS it without a synchronous
/// call back to booking / route / bus / auth.
///
/// Published on the <c>booking.events</c> exchange under
/// <c>Platform.Contracts.EventTypes.BookingConfirmed</c> (<c>booking.confirmed</c>).
/// </summary>
public sealed record BookingConfirmedDomainEvent(
    Guid BookingId,
    Guid TripId,
    Guid CustomerId,
    Guid PaymentId,
    Guid OperatorId,
    string CustomerEmail,
    string CustomerName,
    string? CustomerPhone,
    string OriginCity,
    string DestinationCity,
    DateTimeOffset DepartureUtc,
    DateTimeOffset ArrivalUtc,
    string BusPlateNumber,
    string BusType,
    IReadOnlyCollection<string> SeatNumbers,
    IReadOnlyCollection<string> PassengerNames,
    decimal TotalAmount,
    string Currency) : DomainEvent;
