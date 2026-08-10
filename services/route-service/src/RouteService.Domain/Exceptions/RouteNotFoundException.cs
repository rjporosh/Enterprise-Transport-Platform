namespace RouteService.Domain.Exceptions;

public class RouteNotFoundException : DomainException
{
    public RouteNotFoundException(Guid routeId)
        : base($"Route with id '{routeId}' was not found.") { }
}
