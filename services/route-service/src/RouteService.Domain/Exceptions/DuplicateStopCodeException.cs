namespace RouteService.Domain.Exceptions;

public class DuplicateStopCodeException : DomainException
{
    public DuplicateStopCodeException(string code)
        : base($"Stop with code '{code}' already exists.") { }
}
