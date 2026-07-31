using BookingService.Application.Common.Interfaces;
using BookingService.Application.Common.Models;
using BookingService.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Application.Features.Trips.SearchTrips;

/// <summary>
/// Read-side handler: queries directly via EF Core projections rather than
/// going through the Trip aggregate, per CQRS — reads don't need aggregate
/// invariants, just a fast, shaped DTO.
/// </summary>
public sealed class SearchTripsHandler : IRequestHandler<SearchTripsQuery, PagedResult<TripSearchResultDto>>
{
    private readonly IBookingDbContext _context;

    public SearchTripsHandler(IBookingDbContext context) => _context = context;

    public async Task<PagedResult<TripSearchResultDto>> Handle(SearchTripsQuery request, CancellationToken cancellationToken)
    {
        var dayStart = new DateTimeOffset(request.DepartureDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);

        var query =
            from trip in _context.Trips.AsNoTracking()
            join route in _context.Routes.AsNoTracking() on trip.RouteId equals route.Id
            join bus in _context.Buses.AsNoTracking() on trip.BusId equals bus.Id
            where trip.Status == TripStatus.Scheduled
                  && trip.DepartureUtc >= dayStart && trip.DepartureUtc < dayEnd
                  && route.OriginCity.ToLower() == request.OriginCity.ToLower()
                  && route.DestinationCity.ToLower() == request.DestinationCity.ToLower()
            orderby trip.DepartureUtc
            select new
            {
                trip.Id,
                route.OriginCity,
                route.DestinationCity,
                trip.DepartureUtc,
                trip.ArrivalUtc,
                bus.BusType,
                bus.PlateNumber,
                trip.BasePrice,
                TotalSeats = bus.TotalSeats,
                AvailableSeats = trip.Seats.Count(s => s.Status == SeatStatus.Available)
            };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(t => new TripSearchResultDto(
            t.Id,
            t.OriginCity,
            t.DestinationCity,
            t.DepartureUtc,
            t.ArrivalUtc,
            t.BusType,
            t.PlateNumber,
            t.BasePrice.Amount,
            t.BasePrice.Currency,
            t.AvailableSeats,
            t.TotalSeats)).ToList();

        return new PagedResult<TripSearchResultDto>(dtos, totalCount, request.Page, request.PageSize);
    }
}
