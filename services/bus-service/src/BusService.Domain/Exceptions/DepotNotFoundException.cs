namespace BusService.Domain.Exceptions;

public sealed class DepotNotFoundException(Guid depotId)
    : DomainException($"Depot with ID '{depotId}' was not found.");
