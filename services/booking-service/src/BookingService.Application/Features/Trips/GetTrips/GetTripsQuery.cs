using BookingService.Application.Common.Models;
using BookingService.Application.Features.Trips.CreateTrip;
using MediatR;

namespace BookingService.Application.Features.Trips.GetTrips;

/// <summary>Admin/operator trip list — all scheduled trips, optionally filtered by route or date window.</summary>
public sealed record GetTripsQuery(
    Guid? RouteId = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<TripDto>>;
