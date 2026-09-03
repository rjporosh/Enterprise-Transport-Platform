using BookingService.Application.Common.Models;
using BookingService.Domain.Enums;
using MediatR;

namespace BookingService.Application.Features.Bookings.GetMyBookings;

public sealed record BookingSummaryDto(
    Guid BookingId,
    Guid TripId,
    string OriginCity,
    string DestinationCity,
    DateTimeOffset DepartureUtc,
    BookingStatus Status,
    decimal TotalAmount,
    string Currency,
    IReadOnlyCollection<string> SeatNumbers,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset HoldExpiresAtUtc);

/// <summary>The signed-in customer's own bookings, newest first.</summary>
public sealed record GetMyBookingsQuery(Guid CustomerId, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<BookingSummaryDto>>;
