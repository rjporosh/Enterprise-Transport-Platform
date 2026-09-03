using BookingService.Application.Common.Interfaces;
using BookingService.Application.Features.Bookings.CancelBooking;
using BookingService.Application.Features.Bookings.CreateBooking;
using BookingService.Application.Features.Bookings.GetBookingById;
using BookingService.Application.Features.Bookings.GetBookings;
using BookingService.Application.Features.Bookings.GetMyBookings;
using BookingService.Domain.Enums;
using MediatR;

namespace BookingService.Api.Endpoints;

/// <summary>
/// Booking endpoints. Customer identity + contact are always sourced from the
/// validated token (<see cref="ICurrentUser"/>), never the request body.
/// </summary>
public static class BookingsEndpoints
{
    public static IEndpointRouteBuilder MapBookingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/bookings").WithTags("Bookings").RequireAuthorization();

        group.MapPost("/", async (
                CreateBookingRequest request,
                ICurrentUser currentUser,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateBookingCommand(
                    request.TripId,
                    currentUser.CustomerId ?? Guid.Empty,
                    currentUser.Email ?? string.Empty,
                    currentUser.FullName ?? "Customer",
                    currentUser.PhoneNumber,
                    request.Passengers);

                var booking = await sender.Send(command, cancellationToken);
                return Results.Created($"/api/v1/bookings/{booking.BookingId}", booking);
            })
            .WithName("CreateBooking")
            .WithSummary("Hold seats and create a PendingPayment booking.")
            .WithDescription("Locks the requested seats and creates a booking with a 10-minute hold. " +
                             "Returns 409 if a seat was taken between search and this call.")
            .Produces<BookingDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/mine", async (
                int? page, int? pageSize,
                ICurrentUser currentUser,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(
                    new GetMyBookingsQuery(currentUser.CustomerId ?? Guid.Empty, page ?? 1, pageSize ?? 20),
                    cancellationToken);
                return Results.Ok(result);
            })
            .WithName("GetMyBookings")
            .WithSummary("The signed-in customer's own bookings, newest first.")
            .Produces<object>(StatusCodes.Status200OK);

        group.MapGet("/", async (
                BookingStatus? status, Guid? tripId, Guid? customerId, int? page, int? pageSize,
                ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(
                    new GetBookingsQuery(status, tripId, customerId, page ?? 1, pageSize ?? 20),
                    cancellationToken);
                return Results.Ok(result);
            })
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Operator"))
            .WithName("GetBookings")
            .WithSummary("All bookings (admin / operator), filterable by status / trip / customer.")
            .Produces<object>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/{bookingId:guid}", async (
                Guid bookingId,
                ICurrentUser currentUser,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var booking = await sender.Send(
                    new GetBookingByIdQuery(bookingId, currentUser.CustomerId ?? Guid.Empty, currentUser.IsInRole("Admin") || currentUser.IsInRole("Operator")),
                    cancellationToken);
                return Results.Ok(booking);
            })
            .WithName("GetBookingById")
            .WithSummary("Fetch a single booking by id (own booking, or any booking for an admin).")
            .Produces<BookingDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{bookingId:guid}/cancel", async (
                Guid bookingId,
                CancelBookingRequest request,
                ICurrentUser currentUser,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                await sender.Send(
                    new CancelBookingCommand(
                        bookingId,
                        currentUser.CustomerId ?? Guid.Empty,
                        currentUser.IsInRole("Admin") || currentUser.IsInRole("Operator"),
                        request.Reason),
                    cancellationToken);
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

public sealed record CreateBookingRequest(Guid TripId, IReadOnlyCollection<PassengerDto> Passengers);
public sealed record CancelBookingRequest(string Reason);
