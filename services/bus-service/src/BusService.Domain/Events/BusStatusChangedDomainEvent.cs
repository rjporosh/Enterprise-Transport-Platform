using BusService.Domain.Enums;

namespace BusService.Domain.Events;

public sealed record BusStatusChangedDomainEvent(Guid BusId, BusStatus OldStatus, BusStatus NewStatus) : Common.DomainEvent;
