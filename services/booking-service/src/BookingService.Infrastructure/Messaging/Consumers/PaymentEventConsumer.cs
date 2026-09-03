using System.Text.Json;
using BookingService.Application.Common.Interfaces;
using BookingService.Domain.Entities;
using BookingService.Domain.Enums;
using BookingService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Contracts.Messaging;

namespace BookingService.Infrastructure.Messaging.Consumers;

/// <summary>
/// Completes the reservation lifecycle from payment outcomes.
/// <list type="bullet">
///   <item><c>payment.succeeded</c> → confirm the booking, book its seats,
///   publish <c>booking.confirmed</c> (carrying the full journey + customer
///   snapshot the ticketing / notification services need).</item>
///   <item><c>payment.failed</c> → release the seat hold and cancel the
///   booking so the seats become available again immediately.</item>
/// </list>
/// Idempotent via the inbox table in <see cref="RabbitMqEventConsumer"/>.
/// </summary>
public sealed class PaymentEventConsumer : RabbitMqEventConsumer
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public PaymentEventConsumer(
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<PaymentEventConsumer> logger)
        : base(options, scopeFactory, logger) { }

    protected override string ConsumerName => "payment-events";

    protected override IReadOnlyCollection<(string Exchange, string RoutingKey)> Bindings =>
    [
        ("payment.events", EventTypes.PaymentSucceeded),
        ("payment.events", EventTypes.PaymentFailed)
    ];

    protected override async Task HandleAsync(string routingKey, string body, IServiceScope scope, CancellationToken cancellationToken)
    {
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var events = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
        var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var orderReference = GetString(root, "orderReference") ?? GetString(root, "OrderReference");
        if (!Guid.TryParse(orderReference, out var bookingId))
        {
            // Not a booking-originated payment (or an older event without the
            // field) — nothing for booking-service to do.
            return;
        }

        var booking = await db.Bookings
            .Include(b => b.Seats)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

        if (booking is null)
            return;

        var trip = await db.Trips
            .Include(t => t.Seats)
            .FirstOrDefaultAsync(t => t.Id == booking.TripId, cancellationToken);

        if (trip is null)
            return;

        if (routingKey == EventTypes.PaymentSucceeded)
        {
            if (booking.Status != BookingStatus.PendingPayment)
                return; // already confirmed/cancelled — treat as a duplicate

            var paymentId = Guid.TryParse(GetString(root, "paymentId") ?? GetString(root, "PaymentId"), out var pid) ? pid : Guid.Empty;
            var journey = await BuildJourneyAsync(db, trip, cancellationToken);

            trip.ConfirmSeats(booking.SeatNumbers.ToList());
            booking.Confirm(clock.UtcNow, journey, paymentId);
        }
        else // payment.failed
        {
            if (booking.Status != BookingStatus.PendingPayment)
                return;

            trip.ReleaseSeats(booking.SeatNumbers.ToList());
            booking.Cancel($"Payment failed: {GetString(root, "reason") ?? GetString(root, "Reason") ?? "unknown"}", clock.UtcNow);
        }

        foreach (var domainEvent in booking.DomainEvents)
            await events.EnqueueAsync(domainEvent, cancellationToken);
        booking.ClearDomainEvents();

        // db.SaveChangesAsync() is called by the base consumer, committing the
        // seat mutation + booking state + outbox rows + inbox row atomically.
    }

    private static async Task<TripJourneyInfo> BuildJourneyAsync(BookingDbContext db, Trip trip, CancellationToken ct)
    {
        var route = await db.Routes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == trip.RouteId, ct);
        var bus = await db.Buses.AsNoTracking().FirstOrDefaultAsync(b => b.Id == trip.BusId, ct);

        return new TripJourneyInfo(
            OriginCity: route?.OriginCity ?? "Unknown",
            DestinationCity: route?.DestinationCity ?? "Unknown",
            DepartureUtc: trip.DepartureUtc,
            ArrivalUtc: trip.ArrivalUtc,
            BusPlateNumber: bus?.PlateNumber ?? "N/A",
            BusType: bus?.BusType ?? "Coach",
            OperatorId: bus?.OperatorId ?? Guid.Empty);
    }

    private static string? GetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
