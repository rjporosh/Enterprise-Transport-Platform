using BusService.Domain.Enums;

namespace BusService.Domain.Events;

/// <summary>
/// Published to RabbitMQ (routing key "bus.registered") so downstream
/// services — chiefly Booking Service's local Bus replica (see that
/// service's Entities/Bus.cs) — can create their own read copy without a
/// synchronous call back here.
/// </summary>
public sealed record BusRegisteredDomainEvent(
    Guid BusId,
    Guid OperatorId,
    string PlateNumber,
    BusType BusType,
    int TotalSeats,
    Guid DepotId) : Common.DomainEvent;
