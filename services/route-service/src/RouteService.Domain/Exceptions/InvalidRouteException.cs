namespace RouteService.Domain.Exceptions;

public class InvalidRouteException : DomainException
{
    public InvalidRouteException(string message) : base(message) { }
}
