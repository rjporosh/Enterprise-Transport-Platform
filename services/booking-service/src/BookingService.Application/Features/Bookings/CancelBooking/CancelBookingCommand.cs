using MediatR;

namespace BookingService.Application.Features.Bookings.CancelBooking;

/// <summary>
/// Cancel a booking and release its seats. <paramref name="RequestedByCustomerId"/>
/// and <paramref name="IsAdmin"/> come from the token — a customer may only
/// cancel their own booking.
/// </summary>
public sealed record CancelBookingCommand(Guid BookingId, Guid RequestedByCustomerId, bool IsAdmin, string Reason) : IRequest;
