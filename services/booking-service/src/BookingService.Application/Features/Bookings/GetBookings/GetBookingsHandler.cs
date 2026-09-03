using BookingService.Application.Common.Interfaces;
using BookingService.Application.Common.Models;
using BookingService.Application.Features.Bookings.GetMyBookings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Application.Features.Bookings.GetBookings;

public sealed class GetBookingsHandler : IRequestHandler<GetBookingsQuery, PagedResult<BookingSummaryDto>>
{
    private readonly IBookingDbContext _context;

    public GetBookingsHandler(IBookingDbContext context) => _context = context;

    public async Task<PagedResult<BookingSummaryDto>> Handle(GetBookingsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.PageSize is < 1 or > 200 ? 20 : request.PageSize;

        var bookings = _context.Bookings.AsNoTracking();
        if (request.Status is { } status) bookings = bookings.Where(b => b.Status == status);
        if (request.TripId is { } tripId) bookings = bookings.Where(b => b.TripId == tripId);
        if (request.CustomerId is { } customerId) bookings = bookings.Where(b => b.CustomerId == customerId);

        var baseQuery =
            from booking in bookings
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
