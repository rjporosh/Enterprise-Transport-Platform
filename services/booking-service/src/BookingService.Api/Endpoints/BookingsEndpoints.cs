using BookingService.Application.Features.Bookings.CancelBooking;
using BookingService.Application.Features.Bookings.CreateBooking;
using BookingService.Application.Features.Bookings.GetBookingById;
using MediatR;

namespace BookingService.Api.Endpoints;

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
            .Produces<BookingDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/{bookingId:guid}", async (Guid bookingId, ISender sender, CancellationToken cancellationToken) =>
            {
                var booking = await sender.Send(new GetBookingByIdQuery(bookingId), cancellationToken);
                return Results.Ok(booking);
            })
            .WithName("GetBookingById")
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
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}

public sealed record CancelBookingRequest(Guid CustomerId, string Reason);
