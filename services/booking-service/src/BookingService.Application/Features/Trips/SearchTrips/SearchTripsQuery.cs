using BookingService.Application.Common.Models;
using MediatR;

namespace BookingService.Application.Features.Trips.SearchTrips;

/// <summary>Search for trips between two cities departing on a given date (customer-facing search).</summary>
public sealed record SearchTripsQuery(
    string OriginCity,
    string DestinationCity,
    DateOnly DepartureDate,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<TripSearchResultDto>>;
