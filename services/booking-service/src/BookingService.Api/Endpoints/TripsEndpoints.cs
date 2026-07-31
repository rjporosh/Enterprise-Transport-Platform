using BookingService.Application.Features.Trips.SearchTrips;
using MediatR;
//using Microsoft.OpenApi.Any;

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
            .WithDescription(
                "Public, unauthenticated — anyone can browse trips before signing in. Results are " +
                "cached in Redis for 30s per (origin, destination, date, page, pageSize) combination.")
            .Produces<object>(StatusCodes.Status200OK);
            // .WithOpenApi(operation =>
            // {
            //     operation.Parameters[0].Example = new OpenApiString("Dhaka");       // origin
            //     operation.Parameters[1].Example = new OpenApiString("Chattogram");  // destination
            //     operation.Parameters[2].Example = new OpenApiString("2026-08-15");  // date
            //     operation.Parameters[3].Example = new OpenApiInteger(1);            // page
            //     operation.Parameters[4].Example = new OpenApiInteger(20);           // pageSize

            //     if (operation.Responses.TryGetValue("200", out var ok))
            //     {
            //         ok.Content["application/json"].Example = new OpenApiObject
            //         {
            //             ["items"] = new OpenApiArray
            //             {
            //                 new OpenApiObject
            //                 {
            //                     ["tripId"] = new OpenApiString("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
            //                     ["originCity"] = new OpenApiString("Dhaka"),
            //                     ["destinationCity"] = new OpenApiString("Chattogram"),
            //                     ["departureUtc"] = new OpenApiString("2026-08-15T02:00:00Z"),
            //                     ["arrivalUtc"] = new OpenApiString("2026-08-15T08:00:00Z"),
            //                     ["busType"] = new OpenApiString("AC Sleeper"),
            //                     ["operatorPlateNumber"] = new OpenApiString("DHK-1234"),
            //                     ["pricePerSeat"] = new OpenApiDouble(1500.00),
            //                     ["currency"] = new OpenApiString("BDT"),
            //                     ["availableSeats"] = new OpenApiInteger(24),
            //                     ["totalSeats"] = new OpenApiInteger(36)
            //                 }
            //             },
            //             ["totalCount"] = new OpenApiInteger(1),
            //             ["page"] = new OpenApiInteger(1),
            //             ["pageSize"] = new OpenApiInteger(20),
            //             ["totalPages"] = new OpenApiInteger(1)
            //         };
            //     }

            //     return operation;
            // });

        return app;
    }
}
