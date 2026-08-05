namespace BusService.Domain.Exceptions;

public sealed class BusNotFoundException : DomainException
{
    public BusNotFoundException(Guid busId) : base($"Bus '{busId}' was not found.") { }
}
