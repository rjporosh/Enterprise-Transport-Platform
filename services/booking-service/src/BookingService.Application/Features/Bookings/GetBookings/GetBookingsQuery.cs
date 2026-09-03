using BookingService.Application.Common.Models;
using BookingService.Application.Features.Bookings.GetMyBookings;
using BookingService.Domain.Enums;
using MediatR;

namespace BookingService.Application.Features.Bookings.GetBookings;

/// <summary>Admin/operator view of all bookings, filterable by status / trip / customer.</summary>
public sealed record GetBookingsQuery(
    BookingStatus? Status = null,
    Guid? TripId = null,
    Guid? CustomerId = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<BookingSummaryDto>>;
