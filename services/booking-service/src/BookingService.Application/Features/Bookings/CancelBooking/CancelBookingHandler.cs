using BookingService.Application.Common.Interfaces;
using BookingService.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Application.Features.Bookings.CancelBooking;

/// <summary>
/// Cancels a booking and releases its seats back to the trip in the same
/// transaction, so a cancellation can never "lose" a seat release if the
/// process crashes mid-way.
/// </summary>
public sealed class CancelBookingHandler : IRequestHandler<CancelBookingCommand>
{
    private readonly IBookingDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly IDateTimeProvider _clock;

    public CancelBookingHandler(IBookingDbContext context, IEventPublisher eventPublisher, IDateTimeProvider clock)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _clock = clock;
    }

    public async Task Handle(CancelBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking is null)
            throw new BookingNotFoundException(request.BookingId);

        if (booking.CustomerId != request.RequestedByCustomerId)
            throw new InvalidBookingStateException("You are not permitted to cancel another customer's booking.");

        var trip = await _context.Trips
            .Include(t => t.Seats)
            .FirstOrDefaultAsync(t => t.Id == booking.TripId, cancellationToken);

        if (trip is null)
            throw new TripNotFoundException(booking.TripId);

        var seatNumbers = booking.SeatNumbers;

        booking.Cancel(request.Reason, _clock.UtcNow);
        trip.ReleaseSeats(seatNumbers);

        foreach (var domainEvent in booking.DomainEvents)
            await _eventPublisher.EnqueueAsync(domainEvent, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        booking.ClearDomainEvents();
    }
}
