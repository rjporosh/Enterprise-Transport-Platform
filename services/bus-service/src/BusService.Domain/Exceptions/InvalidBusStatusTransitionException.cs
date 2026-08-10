using BusService.Domain.Enums;

namespace BusService.Domain.Exceptions;

public sealed class InvalidBusStatusTransitionException(BusStatus from, BusStatus to)
    : DomainException($"Invalid status transition from '{from}' to '{to}'.");
