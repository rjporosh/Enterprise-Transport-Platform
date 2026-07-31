namespace BookingService.Domain.Exceptions;

public sealed class BookingNotFoundException : DomainException
{
    public BookingNotFoundException(Guid bookingId)
        : base($"Booking '{bookingId}' was not found.") { }
}
