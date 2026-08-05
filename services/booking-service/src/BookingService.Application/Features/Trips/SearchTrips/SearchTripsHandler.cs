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
///
/// Cache-aside via Redis: trip search is the highest-traffic, most-repeated
/// read on the whole platform (the same route+date gets hammered by every
/// customer looking at that trip) and tolerates a few seconds of staleness
/// — a seat going from 3 -> 2 available a moment late costs nothing, since
/// CreateBookingHandler re-checks real availability transactionally anyway.
/// That's what makes it a safe cache candidate; Booking reads/writes are not
/// cached for exactly the opposite reason.
/// </summary>
public sealed class SearchTripsHandler : IRequestHandler<SearchTripsQuery, PagedResult<TripSearchResultDto>>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    private readonly IBookingDbContext _context;
    private readonly ICacheService _cache;

    public SearchTripsHandler(IBookingDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<PagedResult<TripSearchResultDto>> Handle(SearchTripsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = BuildCacheKey(request);

        var cached = await _cache.GetAsync<PagedResult<TripSearchResultDto>>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var result = await QueryDatabaseAsync(request, cancellationToken);

        await _cache.SetAsync(cacheKey, result, CacheTtl, cancellationToken);

        return result;
    }

    private static string BuildCacheKey(SearchTripsQuery request) =>
        $"trips:search:{request.OriginCity.ToLowerInvariant()}:{request.DestinationCity.ToLowerInvariant()}:{request.DepartureDate:yyyy-MM-dd}:{request.Page}:{request.PageSize}";

    private async Task<PagedResult<TripSearchResultDto>> QueryDatabaseAsync(SearchTripsQuery request, CancellationToken cancellationToken)
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
