using BookingService.Domain.Common;

namespace BookingService.Domain.Entities;

/// <summary>One booked seat, with the passenger travelling in it. Child entity of Booking.</summary>
public class BookingSeat : Entity
{
    public Guid BookingId { get; private set; }
    public string SeatNumber { get; private set; } = default!;
    public string PassengerFullName { get; private set; } = default!;
    public int PassengerAge { get; private set; }
    public string PassengerGender { get; private set; } = default!;

    private BookingSeat() { } // EF Core

    public BookingSeat(Guid id, Guid bookingId, string seatNumber, string passengerFullName, int passengerAge, string passengerGender)
        : base(id)
    {
        BookingId = bookingId;
        SeatNumber = seatNumber;
        PassengerFullName = passengerFullName;
        PassengerAge = passengerAge;
        PassengerGender = passengerGender;
    }
}
