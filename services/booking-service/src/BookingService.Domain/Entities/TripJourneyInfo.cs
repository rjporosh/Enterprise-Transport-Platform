namespace BookingService.Domain.Entities;

/// <summary>
/// Denormalised journey snapshot passed into <see cref="Booking.Confirm"/> so
/// the <see cref="Events.BookingConfirmedDomainEvent"/> can carry everything
/// the ticketing and notification services need to issue and deliver a
/// ticket — without those services calling back into booking / route / bus.
/// </summary>
public sealed record TripJourneyInfo(
    string OriginCity,
    string DestinationCity,
    DateTimeOffset DepartureUtc,
    DateTimeOffset ArrivalUtc,
    string BusPlateNumber,
    string BusType,
    Guid OperatorId);
