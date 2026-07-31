using BookingService.Application.Features.Bookings.CancelBooking;
using BookingService.Application.Features.Bookings.CreateBooking;
using BookingService.Application.Features.Bookings.GetBookingById;
using MediatR;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace BookingService.Api.Endpoints;

public static class BookingsEndpoints
{
    // A real trip id only exists once you've run the seed script (see
    // scripts/seed-demo-data.sql) — these are stand-ins so the example
    // renders something plausible in Swagger/Scalar before you've seeded.
    private const string ExampleTripId = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
    private const string ExampleBookingId = "b2c3d4e5-1234-4a5b-8c9d-0e1f2a3b4c5d";
    private const string ExampleCustomerId = "00000000-0000-0000-0000-000000000001";

    public static IEndpointRouteBuilder MapBookingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/bookings").WithTags("Bookings").RequireAuthorization();

        group.MapPost("/", async (CreateBookingCommand command, ISender sender, CancellationToken cancellationToken) =>
            {
                var booking = await sender.Send(command, cancellationToken);
                return Results.Created($"/api/v1/bookings/{booking.BookingId}", booking);
            })
            .WithName("CreateBooking")
            .WithSummary("Hold seats and create a PendingPayment booking.")
            .WithDescription(
                "Locks the requested seats on the trip and creates a booking in PendingPayment " +
                "status with a 10-minute hold. Returns 409 Conflict if any seat was taken between " +
                "your search and this call — that's the concurrency control working as intended, " +
                "not a bug; re-search and pick a different seat.")
            .Produces<BookingDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithOpenApi(operation =>
            {
                operation.RequestBody.Content["application/json"].Example = new OpenApiObject
                {
                    ["tripId"] = new OpenApiString(ExampleTripId),
                    ["customerId"] = new OpenApiString(ExampleCustomerId),
                    ["passengers"] = new OpenApiArray
                    {
                        new OpenApiObject
                        {
                            ["seatNumber"] = new OpenApiString("A1"),
                            ["fullName"] = new OpenApiString("Porosh Ahmed"),
                            ["age"] = new OpenApiInteger(30),
                            ["gender"] = new OpenApiString("Male")
                        }
                    }
                };

                if (operation.Responses.TryGetValue("201", out var created))
                {
                    created.Content["application/json"].Example = new OpenApiObject
                    {
                        ["bookingId"] = new OpenApiString(ExampleBookingId),
                        ["tripId"] = new OpenApiString(ExampleTripId),
                        ["customerId"] = new OpenApiString(ExampleCustomerId),
                        ["status"] = new OpenApiString("PendingPayment"),
                        ["totalAmount"] = new OpenApiDouble(1500.00),
                        ["currency"] = new OpenApiString("BDT"),
                        ["createdAtUtc"] = new OpenApiString("2026-08-01T09:00:00Z"),
                        ["holdExpiresAtUtc"] = new OpenApiString("2026-08-01T09:10:00Z"),
                        ["seats"] = new OpenApiArray
                        {
                            new OpenApiObject
                            {
                                ["seatNumber"] = new OpenApiString("A1"),
                                ["passengerFullName"] = new OpenApiString("Porosh Ahmed")
                            }
                        }
                    };
                }

                return operation;
            });

        group.MapGet("/{bookingId:guid}", async (Guid bookingId, ISender sender, CancellationToken cancellationToken) =>
            {
                var booking = await sender.Send(new GetBookingByIdQuery(bookingId), cancellationToken);
                return Results.Ok(booking);
            })
            .WithName("GetBookingById")
            .WithSummary("Fetch a single booking by id.")
            .Produces<BookingDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Example = new OpenApiString(ExampleBookingId);
                return operation;
            });

        group.MapPost("/{bookingId:guid}/cancel", async (
                Guid bookingId,
                CancelBookingRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                await sender.Send(new CancelBookingCommand(bookingId, request.CustomerId, request.Reason), cancellationToken);
                return Results.NoContent();
            })
            .WithName("CancelBooking")
            .WithSummary("Cancel a booking and release its seats.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithOpenApi(operation =>
            {
                operation.Parameters[0].Example = new OpenApiString(ExampleBookingId);
                operation.RequestBody.Content["application/json"].Example = new OpenApiObject
                {
                    ["customerId"] = new OpenApiString(ExampleCustomerId),
                    ["reason"] = new OpenApiString("Change of travel plans")
                };
                return operation;
            });

        return app;
    }
}

public sealed record CancelBookingRequest(Guid CustomerId, string Reason);
