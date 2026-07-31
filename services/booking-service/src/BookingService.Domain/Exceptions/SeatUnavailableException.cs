namespace BookingService.Domain.Exceptions;

public sealed class SeatUnavailableException : DomainException
{
    public SeatUnavailableException(string seatNumber, Guid tripId)
        : base($"Seat '{seatNumber}' on trip '{tripId}' is no longer available.") { }
}
