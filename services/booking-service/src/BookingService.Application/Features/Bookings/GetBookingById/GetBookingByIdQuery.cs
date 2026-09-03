using BookingService.Application.Features.Bookings.CreateBooking;
using MediatR;

namespace BookingService.Application.Features.Bookings.GetBookingById;

/// <summary>
/// Fetch one booking. <paramref name="RequestedByCustomerId"/> and
/// <paramref name="IsAdmin"/> are supplied server-side from the token — a
/// non-admin requesting a booking that isn't theirs gets a 404 (not a 403),
/// so the endpoint doesn't leak the existence of other customers' bookings
/// (closes the P0-9 IDOR).
/// </summary>
public sealed record GetBookingByIdQuery(Guid BookingId, Guid RequestedByCustomerId, bool IsAdmin) : IRequest<BookingDto>;
