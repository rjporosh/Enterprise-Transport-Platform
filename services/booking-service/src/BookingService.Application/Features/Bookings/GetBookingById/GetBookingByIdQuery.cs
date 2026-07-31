using BookingService.Application.Features.Bookings.CreateBooking;
using MediatR;

namespace BookingService.Application.Features.Bookings.GetBookingById;

public sealed record GetBookingByIdQuery(Guid BookingId) : IRequest<BookingDto>;
