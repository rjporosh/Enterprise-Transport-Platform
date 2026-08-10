namespace BusService.Domain.Exceptions;

public sealed class ConcurrencyException(string message)
    : DomainException(message);
