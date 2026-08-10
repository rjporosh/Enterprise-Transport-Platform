namespace BusService.Domain.Exceptions;

public sealed class DuplicatePlateNumberException(string plateNumber)
    : DomainException($"A bus with plate number '{plateNumber}' already exists.");
