namespace BusService.Domain.Exceptions;

public sealed class DuplicatePlateNumberException : DomainException
{
    public DuplicatePlateNumberException(string plateNumber)
        : base($"A bus with plate number '{plateNumber}' is already registered.") { }
}
