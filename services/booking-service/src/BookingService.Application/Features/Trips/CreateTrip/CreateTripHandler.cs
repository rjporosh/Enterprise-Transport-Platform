using BookingService.Application.Common.Interfaces;
using BookingService.Domain.Common;
using BookingService.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Application.Features.Trips.CreateTrip;

/// <summary>
/// Upserts the Route + Bus read-model replicas from the inline reference
/// data, then creates the Trip aggregate with generated seat inventory. All
/// in one transaction.
/// </summary>
public sealed class CreateTripHandler : IRequestHandler<CreateTripCommand, TripDto>
{
    private readonly IBookingDbContext _context;
    private readonly ICacheService _cache;

    public CreateTripHandler(IBookingDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<TripDto> Handle(CreateTripCommand request, CancellationToken cancellationToken)
    {
        await UpsertRouteAsync(request, cancellationToken);
        await UpsertBusAsync(request, cancellationToken);

        var seatLayout = (request.SeatMap is { Count: > 0 } map
                ? map.Select(s => (s.SeatNumber, s.Deck))
                : GenerateSeatLayout(request.TotalSeats))
            .ToList();

        var trip = new Trip(
            Guid.NewGuid(),
            request.RouteId,
            request.BusId,
            request.DepartureUtc,
            request.ArrivalUtc,
            new Money(request.BasePrice, request.Currency.ToUpperInvariant()),
            seatLayout);

        _context.Trips.Add(trip);
        await _context.SaveChangesAsync(cancellationToken);

        await _cache.RemoveByPrefixAsync("trips:search:", cancellationToken);

        return new TripDto(
            trip.Id, trip.RouteId, trip.BusId,
            request.OriginCity, request.DestinationCity,
            trip.DepartureUtc, trip.ArrivalUtc,
            trip.BasePrice.Amount, trip.BasePrice.Currency,
            seatLayout.Count, trip.Status.ToString());
    }

    private async Task UpsertRouteAsync(CreateTripCommand request, CancellationToken ct)
    {
        var route = await _context.Routes.FirstOrDefaultAsync(r => r.Id == request.RouteId, ct);
        if (route is null)
            _context.Routes.Add(new Route(request.RouteId, request.OriginCity, request.DestinationCity, request.DistanceKm));
        // Route replica is immutable reference data keyed by id; if it exists we keep it.
    }

    private async Task UpsertBusAsync(CreateTripCommand request, CancellationToken ct)
    {
        var bus = await _context.Buses.FirstOrDefaultAsync(b => b.Id == request.BusId, ct);
        if (bus is null)
            _context.Buses.Add(new Bus(request.BusId, request.OperatorId, request.BusPlateNumber, request.BusType, request.TotalSeats));
    }

    /// <summary>Generates seat numbers in a 4-across coach layout: <c>1A 1B 1C 1D 2A …</c>, all on the lower deck.</summary>
    private static IEnumerable<(string SeatNumber, string Deck)> GenerateSeatLayout(int totalSeats)
    {
        const string columns = "ABCD";
        for (var i = 0; i < totalSeats; i++)
        {
            var row = i / 4 + 1;
            var col = columns[i % 4];
            yield return ($"{row}{col}", "Lower");
        }
    }
}
