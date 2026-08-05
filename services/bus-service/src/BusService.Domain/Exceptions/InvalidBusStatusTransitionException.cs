using BusService.Domain.Enums;

namespace BusService.Domain.Exceptions;

public sealed class InvalidBusStatusTransitionException : DomainException
{
    public InvalidBusStatusTransitionException(BusStatus from, BusStatus to)
        : base($"Cannot transition a bus from {from} to {to}.") { }
}
