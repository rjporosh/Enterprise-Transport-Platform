namespace NotificationService.Domain.Exceptions;

/// <summary>Base type for all Notification Service domain exceptions — lets ExceptionHandlingMiddleware map any of them to HTTP 400 by default while specific subclasses override with a more precise status code.</summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}
