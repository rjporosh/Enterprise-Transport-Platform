using BookingService.Application.Features.Trips.SearchTrips;
using MediatR;

namespace BookingService.Api.Endpoints;

public static class TripsEndpoints
{
    public static IEndpointRouteBuilder MapTripsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/trips").WithTags("Trips");

        group.MapGet("/search", async (
                string origin,
                string destination,
                DateOnly date,
                int page,
                int pageSize,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var query = new SearchTripsQuery(
                    origin,
                    destination,
                    date,
                    page == 0 ? 1 : page,
                    pageSize == 0 ? 20 : pageSize);

                var result = await sender.Send(query, cancellationToken);
                return Results.Ok(result);
            })
            .WithName("SearchTrips")
            .WithSummary("Search scheduled trips between two cities on a given date.")
            .Produces<object>(StatusCodes.Status200OK);

        return app;
    }
}
