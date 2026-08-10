using BusService.Domain.Enums;

namespace BusService.Domain.Events;

public sealed record BusRegisteredDomainEvent(
    Guid BusId,
    Guid OperatorId,
    string PlateNumber,
    BusType BusType,
    int TotalSeats,
    Guid DepotId) : Common.DomainEvent;
