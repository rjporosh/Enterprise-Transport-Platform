using MediatR;

namespace BookingService.Application.Features.Trips.GetTripById;

public sealed record SeatDto(string SeatNumber, string Deck, string Status);

public sealed record TripDetailDto(
    Guid TripId,
    Guid RouteId,
    Guid BusId,
    string OriginCity,
    string DestinationCity,
    string BusPlateNumber,
    string BusType,
    DateTimeOffset DepartureUtc,
    DateTimeOffset ArrivalUtc,
    decimal BasePrice,
    string Currency,
    string Status,
    int TotalSeats,
    int AvailableSeats,
    IReadOnlyCollection<SeatDto> Seats);

/// <summary>
/// The full seat map for one trip — every seat with its live
/// <c>Available | Held | Booked</c> status. Backs the customer seat-selection
/// screen.
/// </summary>
public sealed record GetTripByIdQuery(Guid TripId) : IRequest<TripDetailDto>;
