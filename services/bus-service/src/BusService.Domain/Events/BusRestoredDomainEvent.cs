namespace BusService.Domain.Events;

public sealed record BusRestoredDomainEvent(Guid BusId) : Common.DomainEvent;
