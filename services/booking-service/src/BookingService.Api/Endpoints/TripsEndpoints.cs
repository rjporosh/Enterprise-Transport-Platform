using System.Text.Json;
using BookingService.Application.Features.Trips.CreateTrip;
using BookingService.Application.Features.Trips.GetTripById;
using BookingService.Application.Features.Trips.GetTrips;
using BookingService.Application.Features.Trips.SearchTrips;
using MediatR;

namespace BookingService.Api.Endpoints;

public static class TripsEndpoints
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 10;

    public static IEndpointRouteBuilder MapTripsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/trips").WithTags("Trips");

        // ---- Public search -------------------------------------------------
        group.MapGet("/search", async (
                string origin,
                string destination,
                DateOnly date,
                int? page,
                int? pageSize,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var effectivePage = page is null or <= 0 ? DefaultPage : page.Value;
                var effectivePageSize = pageSize is null or <= 0 ? DefaultPageSize : pageSize.Value;

                var result = await sender.Send(
                    new SearchTripsQuery(origin, destination, date, effectivePage, effectivePageSize),
                    cancellationToken);

                httpContext.Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(new
                {
                    currentPage = result.Page,
                    pageSize = result.PageSize,
                    totalCount = result.TotalCount,
                    totalPages = result.TotalPages,
                    hasPrevious = result.Page > 1,
                    hasNext = result.Page < result.TotalPages
                }));
                httpContext.Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());

                return Results.Ok(result);
            })
            .WithName("SearchTrips")
            .WithSummary("Search scheduled trips between two cities on a given date.")
            .WithDescription("Public, unauthenticated. Cached in Redis for 30s per (origin, destination, date, page, pageSize). " +
                             "Example: /api/v1/trips/search?origin=Dhaka&destination=Chattogram&date=2026-09-20")
            .Produces<object>(StatusCodes.Status200OK);

        // ---- Seat map (public — customers browse before signing in) -------
        group.MapGet("/{tripId:guid}", async (Guid tripId, ISender sender, CancellationToken cancellationToken) =>
            {
                var trip = await sender.Send(new GetTripByIdQuery(tripId), cancellationToken);
                return Results.Ok(trip);
            })
            .WithName("GetTripById")
            .WithSummary("Full seat map for one trip (every seat + Available/Held/Booked status).")
            .Produces<TripDetailDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // ---- Admin: schedule + list --------------------------------------
        var admin = app.MapGroup("/api/v1/trips").WithTags("Trips (admin)")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Operator"));

        admin.MapPost("/", async (CreateTripCommand command, ISender sender, CancellationToken cancellationToken) =>
            {
                var trip = await sender.Send(command, cancellationToken);
                return Results.Created($"/api/v1/trips/{trip.TripId}", trip);
            })
            .WithName("CreateTrip")
            .WithSummary("Schedule a departure of a bus along a route (generates seat inventory).")
            .WithDescription("Requires the Admin or Operator role. Route + bus reference data is carried inline and " +
                             "upserted into booking-service's local read-model.")
            .Produces<TripDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        admin.MapGet("/", async (
                Guid? routeId, DateOnly? fromDate, DateOnly? toDate, int? page, int? pageSize,
                ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(
                    new GetTripsQuery(routeId, fromDate, toDate, page ?? 1, pageSize ?? 20),
                    cancellationToken);
                return Results.Ok(result);
            })
            .WithName("GetTrips")
            .WithSummary("List scheduled trips (admin / operator).")
            .Produces<object>(StatusCodes.Status200OK);

        return app;
    }
}
