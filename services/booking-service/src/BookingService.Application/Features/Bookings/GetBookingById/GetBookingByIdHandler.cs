using BookingService.Application.Common.Interfaces;
using BookingService.Application.Features.Bookings.CreateBooking;
using BookingService.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Application.Features.Bookings.GetBookingById;

public sealed class GetBookingByIdHandler : IRequestHandler<GetBookingByIdQuery, BookingDto>
{
    private readonly IBookingDbContext _context;

    public GetBookingByIdHandler(IBookingDbContext context) => _context = context;

    public async Task<BookingDto> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
    {
        var booking = await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Seats)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        // Ownership check folded into the "not found" path on purpose: a
        // customer asking for someone else's booking must not be able to tell
        // it exists.
        if (booking is null || (!request.IsAdmin && booking.CustomerId != request.RequestedByCustomerId))
            throw new BookingNotFoundException(request.BookingId);

        return new BookingDto(
            booking.Id,
            booking.TripId,
            booking.CustomerId,
            booking.Status,
            booking.TotalAmount.Amount,
            booking.TotalAmount.Currency,
            booking.CreatedAtUtc,
            booking.HoldExpiresAtUtc,
            booking.Seats.Select(s => new BookingSeatDto(s.SeatNumber, s.PassengerFullName)).ToList());
    }
}
