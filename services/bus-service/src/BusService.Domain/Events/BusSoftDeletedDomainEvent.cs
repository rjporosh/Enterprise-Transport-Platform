namespace BusService.Domain.Events;

public sealed record BusSoftDeletedDomainEvent(Guid BusId, string DeletedBy) : Common.DomainEvent;
