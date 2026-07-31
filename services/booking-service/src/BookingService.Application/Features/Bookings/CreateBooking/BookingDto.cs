using BookingService.Domain.Enums;

namespace BookingService.Application.Features.Bookings.CreateBooking;

public sealed record BookingSeatDto(string SeatNumber, string PassengerFullName);

public sealed record BookingDto(
    Guid BookingId,
    Guid TripId,
    Guid CustomerId,
    BookingStatus Status,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset HoldExpiresAtUtc,
    IReadOnlyCollection<BookingSeatDto> Seats);
