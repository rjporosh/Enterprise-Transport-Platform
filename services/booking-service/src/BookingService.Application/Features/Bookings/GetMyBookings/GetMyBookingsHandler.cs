using BookingService.Application.Common.Interfaces;
using BookingService.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Application.Features.Bookings.GetMyBookings;

public sealed class GetMyBookingsHandler : IRequestHandler<GetMyBookingsQuery, PagedResult<BookingSummaryDto>>
{
    private readonly IBookingDbContext _context;

    public GetMyBookingsHandler(IBookingDbContext context) => _context = context;

    public async Task<PagedResult<BookingSummaryDto>> Handle(GetMyBookingsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var baseQuery =
            from booking in _context.Bookings.AsNoTracking()
            where booking.CustomerId == request.CustomerId
            join trip in _context.Trips.AsNoTracking() on booking.TripId equals trip.Id into tj
            from trip in tj.DefaultIfEmpty()
            join route in _context.Routes.AsNoTracking() on trip.RouteId equals route.Id into rj
            from route in rj.DefaultIfEmpty()
            orderby booking.CreatedAtUtc descending
            select new { booking, trip, route };

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var rows = await baseQuery
            .Skip((page - 1) * size)
            .Take(size)
            .Select(x => new
            {
                x.booking.Id,
                x.booking.TripId,
                OriginCity = x.route != null ? x.route.OriginCity : "—",
                DestinationCity = x.route != null ? x.route.DestinationCity : "—",
                DepartureUtc = x.trip != null ? x.trip.DepartureUtc : x.booking.CreatedAtUtc,
                x.booking.Status,
                Amount = x.booking.TotalAmount.Amount,
                Currency = x.booking.TotalAmount.Currency,
                Seats = x.booking.Seats.Select(s => s.SeatNumber).ToList(),
                x.booking.CreatedAtUtc,
                x.booking.HoldExpiresAtUtc
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(r => new BookingSummaryDto(
            r.Id, r.TripId, r.OriginCity, r.DestinationCity, r.DepartureUtc,
            r.Status, r.Amount, r.Currency, r.Seats, r.CreatedAtUtc, r.HoldExpiresAtUtc)).ToList();

        return new PagedResult<BookingSummaryDto>(items, totalCount, page, size);
    }
}
