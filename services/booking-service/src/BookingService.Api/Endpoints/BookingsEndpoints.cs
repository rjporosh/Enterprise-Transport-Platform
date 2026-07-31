using BookingService.Application.Features.Bookings.CancelBooking;
using BookingService.Application.Features.Bookings.CreateBooking;
using BookingService.Application.Features.Bookings.GetBookingById;
using MediatR;

namespace BookingService.Api.Endpoints;

/// <summary>
/// Booking endpoints. Example request/response payloads for these routes
/// live in docs/API_EXAMPLES.md and the Postman collection (postman/) rather
/// than inline OpenApi.NET "Any" objects here — those types moved/changed
/// shape between OpenAPI.NET v1 and v2 as part of the .NET 10 upgrade and
/// aren't worth re-coupling this file to; Scalar renders the schema fine
/// from WithSummary/WithDescription/Produces alone.
/// </summary>
public static class BookingsEndpoints
{
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
                "not a bug; re-search and pick a different seat. See docs/API_EXAMPLES.md for a " +
                "full request/response sample, or the Postman collection under postman/.")
            .Produces<BookingDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/{bookingId:guid}", async (Guid bookingId, ISender sender, CancellationToken cancellationToken) =>
            {
                var booking = await sender.Send(new GetBookingByIdQuery(bookingId), cancellationToken);
                return Results.Ok(booking);
            })
            .WithName("GetBookingById")
            .WithSummary("Fetch a single booking by id.")
            .Produces<BookingDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

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
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}

public sealed record CancelBookingRequest(Guid CustomerId, string Reason);
