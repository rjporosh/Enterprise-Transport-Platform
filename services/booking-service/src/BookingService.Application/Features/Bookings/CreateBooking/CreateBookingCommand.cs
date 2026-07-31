using MediatR;

namespace BookingService.Application.Features.Bookings.CreateBooking;

public sealed record PassengerDto(string SeatNumber, string FullName, int Age, string Gender);

/// <summary>
/// Creates a PendingPayment booking and holds the requested seats on the
/// trip. The booking expires (and seats are released) after 10 minutes if
/// payment isn't completed — see Booking.HoldExpiresAtUtc.
/// </summary>
public sealed record CreateBookingCommand(
    Guid TripId,
    Guid CustomerId,
    IReadOnlyCollection<PassengerDto> Passengers) : IRequest<BookingDto>;
