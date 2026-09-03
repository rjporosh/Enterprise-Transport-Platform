using BookingService.Application.Common.Interfaces;
using BookingService.Domain.Enums;
using BookingService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace BookingService.Infrastructure.Scheduling;

/// <summary>
/// Releases seats held by bookings whose 10-minute payment window lapsed
/// without a <c>payment.succeeded</c>. Runs every 60s. Each booking is
/// expired + its seats released in one transaction; a
/// <c>booking.cancelled</c> event is published so the customer is told the
/// hold expired.
///
/// <see cref="DisallowConcurrentExecutionAttribute"/> keeps a slow run from
/// overlapping the next trigger. Cluster-wide single execution (Quartz
/// persistent store + clustering) is deferred to M9 — until then run a
/// single booking-service replica for the sweep, or accept that a duplicate
/// release is a no-op (seats already Available).
/// </summary>
[DisallowConcurrentExecution]
public sealed class ExpiredHoldSweepJob : IJob
{
    private const int BatchSize = 100;

    private readonly BookingDbContext _db;
    private readonly IEventPublisher _events;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<ExpiredHoldSweepJob> _logger;

    public ExpiredHoldSweepJob(
        BookingDbContext db,
        IEventPublisher events,
        IDateTimeProvider clock,
        ILogger<ExpiredHoldSweepJob> logger)
    {
        _db = db;
        _events = events;
        _clock = clock;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var now = _clock.UtcNow;

        var expired = await _db.Bookings
            .Include(b => b.Seats)
            .Where(b => b.Status == BookingStatus.PendingPayment && b.HoldExpiresAtUtc < now)
            .OrderBy(b => b.HoldExpiresAtUtc)
            .Take(BatchSize)
            .ToListAsync(context.CancellationToken);

        if (expired.Count == 0)
            return;

        foreach (var booking in expired)
        {
            var trip = await _db.Trips
                .Include(t => t.Seats)
                .FirstOrDefaultAsync(t => t.Id == booking.TripId, context.CancellationToken);

            trip?.ReleaseSeats(booking.SeatNumbers.ToList());
            booking.Expire(now);

            foreach (var domainEvent in booking.DomainEvents)
                await _events.EnqueueAsync(domainEvent, context.CancellationToken);
            booking.ClearDomainEvents();
        }

        await _db.SaveChangesAsync(context.CancellationToken);
        _logger.LogInformation("Expired {Count} unpaid booking hold(s).", expired.Count);
    }
}
