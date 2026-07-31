using MediatR;

namespace BookingService.Application.Features.Bookings.CancelBooking;

public sealed record CancelBookingCommand(Guid BookingId, Guid RequestedByCustomerId, string Reason) : IRequest;
