namespace RouteService.Domain.Exceptions;

public class StopNotFoundException : DomainException
{
    public StopNotFoundException(Guid stopId)
        : base($"Stop with id '{stopId}' was not found.") { }

    public StopNotFoundException(string code)
        : base($"Stop with code '{code}' was not found.") { }
}
