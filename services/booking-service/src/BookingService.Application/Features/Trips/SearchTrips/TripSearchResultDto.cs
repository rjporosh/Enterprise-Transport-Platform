namespace BookingService.Application.Features.Trips.SearchTrips;

public sealed record TripSearchResultDto(
    Guid TripId,
    string OriginCity,
    string DestinationCity,
    DateTimeOffset DepartureUtc,
    DateTimeOffset ArrivalUtc,
    string BusType,
    string OperatorPlateNumber,
    decimal PricePerSeat,
    string Currency,
    int AvailableSeats,
    int TotalSeats);
