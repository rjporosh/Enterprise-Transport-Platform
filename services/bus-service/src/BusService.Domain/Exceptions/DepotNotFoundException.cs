namespace BusService.Domain.Exceptions;

public sealed class DepotNotFoundException : DomainException
{
    public DepotNotFoundException(Guid depotId) : base($"Depot '{depotId}' was not found.") { }
}
