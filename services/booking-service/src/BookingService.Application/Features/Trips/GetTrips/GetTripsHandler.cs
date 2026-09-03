using BookingService.Application.Common.Interfaces;
using BookingService.Application.Common.Models;
using BookingService.Application.Features.Trips.CreateTrip;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Application.Features.Trips.GetTrips;

public sealed class GetTripsHandler : IRequestHandler<GetTripsQuery, PagedResult<TripDto>>
{
    private readonly IBookingDbContext _context;

    public GetTripsHandler(IBookingDbContext context) => _context = context;

    public async Task<PagedResult<TripDto>> Handle(GetTripsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.PageSize is < 1 or > 200 ? 20 : request.PageSize;

        var trips = _context.Trips.AsNoTracking();
        if (request.RouteId is { } routeId) trips = trips.Where(t => t.RouteId == routeId);
        if (request.FromDate is { } from)
        {
            var fromUtc = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            trips = trips.Where(t => t.DepartureUtc >= fromUtc);
        }
        if (request.ToDate is { } to)
        {
            var toUtc = new DateTimeOffset(to.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(1);
            trips = trips.Where(t => t.DepartureUtc < toUtc);
        }

        var baseQuery =
            from trip in trips
            join route in _context.Routes.AsNoTracking() on trip.RouteId equals route.Id into rj
            from route in rj.DefaultIfEmpty()
            orderby trip.DepartureUtc
            select new { trip, route };

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var rows = await baseQuery
            .Skip((page - 1) * size)
            .Take(size)
            .Select(x => new
            {
                x.trip.Id,
                x.trip.RouteId,
                x.trip.BusId,
                OriginCity = x.route != null ? x.route.OriginCity : "—",
                DestinationCity = x.route != null ? x.route.DestinationCity : "—",
                x.trip.DepartureUtc,
                x.trip.ArrivalUtc,
                Amount = x.trip.BasePrice.Amount,
                Currency = x.trip.BasePrice.Currency,
                TotalSeats = x.trip.Seats.Count,
                Status = x.trip.Status
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(r => new TripDto(
            r.Id, r.RouteId, r.BusId, r.OriginCity, r.DestinationCity,
            r.DepartureUtc, r.ArrivalUtc, r.Amount, r.Currency, r.TotalSeats, r.Status.ToString())).ToList();

        return new PagedResult<TripDto>(items, totalCount, page, size);
    }
}
