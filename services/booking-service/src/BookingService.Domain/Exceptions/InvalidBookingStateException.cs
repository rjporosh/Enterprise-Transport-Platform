namespace BookingService.Domain.Exceptions;

public sealed class InvalidBookingStateException : DomainException
{
    public InvalidBookingStateException(string message) : base(message) { }
}
