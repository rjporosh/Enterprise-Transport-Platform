namespace BusService.Domain.Exceptions;

public sealed class BusNotFoundException(Guid busId)
    : DomainException($"Bus with ID '{busId}' was not found.");
