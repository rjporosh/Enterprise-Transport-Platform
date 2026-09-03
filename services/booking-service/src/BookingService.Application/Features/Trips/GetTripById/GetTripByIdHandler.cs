using BookingService.Application.Common.Interfaces;
using BookingService.Domain.Enums;
using BookingService.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Application.Features.Trips.GetTripById;

public sealed class GetTripByIdHandler : IRequestHandler<GetTripByIdQuery, TripDetailDto>
{
    private readonly IBookingDbContext _context;

    public GetTripByIdHandler(IBookingDbContext context) => _context = context;

    public async Task<TripDetailDto> Handle(GetTripByIdQuery request, CancellationToken cancellationToken)
    {
        var trip = await _context.Trips
            .AsNoTracking()
            .Include(t => t.Seats)
            .FirstOrDefaultAsync(t => t.Id == request.TripId, cancellationToken);

        if (trip is null)
            throw new TripNotFoundException(request.TripId);

        var route = await _context.Routes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == trip.RouteId, cancellationToken);
        var bus = await _context.Buses.AsNoTracking().FirstOrDefaultAsync(b => b.Id == trip.BusId, cancellationToken);

        var seats = trip.Seats
            .OrderBy(s => s.Deck)
            .ThenBy(s => s.SeatNumber, StringComparer.OrdinalIgnoreCase)
            .Select(s => new SeatDto(s.SeatNumber, s.Deck, s.Status.ToString()))
            .ToList();

        return new TripDetailDto(
            trip.Id,
            trip.RouteId,
            trip.BusId,
            route?.OriginCity ?? "—",
            route?.DestinationCity ?? "—",
            bus?.PlateNumber ?? "—",
            bus?.BusType ?? "—",
            trip.DepartureUtc,
            trip.ArrivalUtc,
            trip.BasePrice.Amount,
            trip.BasePrice.Currency,
            trip.Status.ToString(),
            seats.Count,
            seats.Count(s => s.Status == nameof(SeatStatus.Available)),
            seats);
    }
}
