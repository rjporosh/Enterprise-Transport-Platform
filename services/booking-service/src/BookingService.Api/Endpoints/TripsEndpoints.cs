using System.Text.Json;
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
                // If the caller omits page/pageSize entirely, fall back to
                // page 1 / 10 results per page rather than returning
                // everything unpaginated.
                var effectivePage = page is null or <= 0 ? DefaultPage : page.Value;
                var effectivePageSize = pageSize is null or <= 0 ? DefaultPageSize : pageSize.Value;

                var query = new SearchTripsQuery(origin, destination, date, effectivePage, effectivePageSize);
                var result = await sender.Send(query, cancellationToken);

                // Pagination metadata goes in a response header (X-Pagination),
                // the same pattern used across the platform's other list
                // endpoints — keeps the JSON body as just the data, and lets
                // clients that don't care about paging ignore the header
                // entirely. See docs/API_PAGINATION.md for the exact shape.
                var paginationHeader = JsonSerializer.Serialize(new
                {
                    currentPage = result.Page,
                    pageSize = result.PageSize,
                    totalCount = result.TotalCount,
                    totalPages = result.TotalPages,
                    hasPrevious = result.Page > 1,
                    hasNext = result.Page < result.TotalPages
                });
                httpContext.Response.Headers.Append("X-Pagination", paginationHeader);
                httpContext.Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());

                return Results.Ok(result);
            })
            .WithName("SearchTrips")
            .WithSummary("Search scheduled trips between two cities on a given date.")
            .WithDescription(
                "Public, unauthenticated — anyone can browse trips before signing in. " +
                "Results are cached in Redis for 30s per (origin, destination, date, page, pageSize) " +
                "combination. Omit page/pageSize to get page 1 of 10 results; pagination metadata " +
                "is also returned in the X-Pagination response header. " +
                "Example: /api/v1/trips/search?origin=Dhaka&destination=Chattogram&date=2026-08-15")
            .Produces<object>(StatusCodes.Status200OK);

        return app;
    }
}
