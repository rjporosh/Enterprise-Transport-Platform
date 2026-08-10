namespace RouteService.Domain.Exceptions;

public class DuplicateRouteCodeException : DomainException
{
    public DuplicateRouteCodeException(string code)
        : base($"Route with code '{code}' already exists.") { }
}
