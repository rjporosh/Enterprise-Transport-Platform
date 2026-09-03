using MediatR;

namespace BookingService.Application.Features.Trips.CreateTrip;

public sealed record TripDto(
    Guid TripId,
    Guid RouteId,
    Guid BusId,
    string OriginCity,
    string DestinationCity,
    DateTimeOffset DepartureUtc,
    DateTimeOffset ArrivalUtc,
    decimal BasePrice,
    string Currency,
    int TotalSeats,
    string Status);

/// <summary>
/// Schedules one departure of a bus along a route (admin / operator).
/// Carries the route + bus reference data inline so booking-service's local
/// read-model replicas (<c>Route</c>, <c>Bus</c>) are upserted as part of
/// trip creation — no separate cross-service sync is needed for the demo /
/// MVP path. Seat inventory is generated from <see cref="TotalSeats"/> using
/// a 4-per-row layout (<c>1A..1D, 2A..</c>) unless <see cref="SeatMap"/> is
/// supplied.
/// </summary>
public sealed record CreateTripCommand(
    Guid RouteId,
    Guid BusId,
    string OriginCity,
    string DestinationCity,
    decimal DistanceKm,
    Guid OperatorId,
    string BusPlateNumber,
    string BusType,
    int TotalSeats,
    DateTimeOffset DepartureUtc,
    DateTimeOffset ArrivalUtc,
    decimal BasePrice,
    string Currency,
    IReadOnlyCollection<SeatSpec>? SeatMap = null) : IRequest<TripDto>;

public sealed record SeatSpec(string SeatNumber, string Deck);
