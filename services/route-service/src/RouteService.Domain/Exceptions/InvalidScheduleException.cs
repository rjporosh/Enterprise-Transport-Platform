namespace RouteService.Domain.Exceptions;

public class InvalidScheduleException : DomainException
{
    public InvalidScheduleException(string message) : base(message) { }
}
