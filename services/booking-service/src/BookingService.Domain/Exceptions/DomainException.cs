namespace BookingService.Domain.Exceptions;

/// <summary>Base type for exceptions that represent a violated business rule.</summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}
