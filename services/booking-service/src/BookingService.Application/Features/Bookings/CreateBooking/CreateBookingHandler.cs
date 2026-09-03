using BookingService.Application.Common.Interfaces;
using BookingService.Domain.Entities;
using BookingService.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BookingService.Application.Features.Bookings.CreateBooking;

/// <summary>
/// The concurrency-critical write path: two customers racing for the same
/// seat must never both succeed. Strategy:
///   1. Load the Trip aggregate (tracked) with its seats.
///   2. Trip.HoldSeats() flips Available -&gt; Held in memory, throwing
///      SeatUnavailableException immediately if the seat is already taken
///      *as of the data we read*.
///   3. Create the Booking aggregate and enqueue its domain events to the
///      outbox — all against the SAME DbContext instance.
///   4. A single SaveChangesAsync() commits the trip seat mutation, the new
///      booking, and the outbox row atomically. EF Core's optimistic
///      concurrency check (Trip.Version -&gt; Postgres `xmin`) means that if
///      another request already committed a conflicting seat hold between
///      our read and our write, this throws DbUpdateConcurrencyException —
///      which we translate into the same SeatUnavailableException the caller
///      already knows how to handle as a 409 Conflict.
/// </summary>
public sealed class CreateBookingHandler : IRequestHandler<CreateBookingCommand, BookingDto>
{
    private readonly IBookingDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly IDateTimeProvider _clock;
    private readonly ICacheService _cache;
    private readonly IBookingMetrics _metrics;
    private readonly ILogger<CreateBookingHandler> _logger;

    public CreateBookingHandler(
        IBookingDbContext context,
        IEventPublisher eventPublisher,
        IDateTimeProvider clock,
        ICacheService cache,
        IBookingMetrics metrics,
        ILogger<CreateBookingHandler> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _clock = clock;
        _cache = cache;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<BookingDto> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        var trip = await _context.Trips
            .Include(t => t.Seats)
            .FirstOrDefaultAsync(t => t.Id == request.TripId, cancellationToken);

        if (trip is null)
            throw new TripNotFoundException(request.TripId);

        var seatNumbers = request.Passengers.Select(p => p.SeatNumber).ToList();

        // Throws SeatUnavailableException synchronously if any seat is already
        // Held/Booked based on what we just read.
        try
        {
            trip.HoldSeats(seatNumbers);
        }
        catch (SeatUnavailableException)
        {
            _metrics.RecordSeatConflict();
            throw;
        }

        var now = _clock.UtcNow;
        var booking = Booking.Create(
            trip.Id,
            request.CustomerId,
            request.CustomerEmail,
            request.CustomerName,
            request.CustomerPhone,
            trip.BasePrice,
            request.Passengers.Select(p => (p.SeatNumber, p.FullName, p.Age, p.Gender)).ToList(),
            now);

        _context.Bookings.Add(booking);

        foreach (var domainEvent in booking.DomainEvents)
            await _eventPublisher.EnqueueAsync(domainEvent, cancellationToken);

        try
        {
            // One transaction: trip seat holds + new booking + outbox row.
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex,
                "Concurrent seat hold conflict on trip {TripId} for seats {SeatNumbers}",
                trip.Id, string.Join(",", seatNumbers));
            _metrics.RecordSeatConflict();

            // Someone else committed a conflicting hold between our read and our
            // write. Surface it as the same business error a same-transaction
            // conflict would produce, so the API/frontend handle it identically.
            throw new SeatUnavailableException(seatNumbers.First(), trip.Id);
        }

        booking.ClearDomainEvents();

        // Available-seat counts just changed for this trip; drop cached search
        // results rather than serving a stale count for the next 30s.
        await _cache.RemoveByPrefixAsync("trips:search:", cancellationToken);
        _metrics.RecordBookingCreated(booking.TotalAmount.Amount, booking.TotalAmount.Currency);

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
