using BusService.Domain.Enums;

namespace BusService.Domain.Events;

/// <summary>Raised on any change to the fields Booking Service's replica cares about (plate number, bus type, seat count) — NOT for status changes, which have their own event.</summary>
public sealed record BusDetailsUpdatedDomainEvent(Guid BusId, string PlateNumber, BusType BusType, int TotalSeats) : Common.DomainEvent;
