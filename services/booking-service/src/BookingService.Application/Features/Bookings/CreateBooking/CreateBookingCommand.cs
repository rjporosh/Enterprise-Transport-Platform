using MediatR;

namespace BookingService.Application.Features.Bookings.CreateBooking;

public sealed record PassengerDto(string SeatNumber, string FullName, int Age, string Gender);

/// <summary>
/// Creates a PendingPayment booking and holds the requested seats on the
/// trip. The booking expires (and seats are released) after 10 minutes if
/// payment isn't completed — see Booking.HoldExpiresAtUtc and
/// <c>ExpiredHoldSweepJob</c>.
///
/// Customer identity and contact are sourced server-side from the
/// authenticated token (<c>ICurrentUser</c>) by the endpoint — never from the
/// request body — closing the P0-10 "customer id from request body" IDOR.
/// </summary>
public sealed record CreateBookingCommand(
    Guid TripId,
    Guid CustomerId,
    string CustomerEmail,
    string CustomerName,
    string? CustomerPhone,
    IReadOnlyCollection<PassengerDto> Passengers) : IRequest<BookingDto>;
