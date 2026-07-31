namespace BookingService.Domain.Exceptions;

public sealed class TripNotFoundException : DomainException
{
    public TripNotFoundException(Guid tripId)
        : base($"Trip '{tripId}' was not found.") { }
}
