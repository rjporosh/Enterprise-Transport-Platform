using BusService.Domain.Common;
using BusService.Domain.Enums;
using BusService.Domain.Events;
using BusService.Domain.Exceptions;

namespace BusService.Domain.Entities;

/// <summary>
/// Aggregate root for a single vehicle in the fleet. This is the canonical
/// (source-of-truth) definition — Booking Service keeps a read-only,
/// denormalized replica of the fields it needs (OperatorId, PlateNumber,
/// BusType, TotalSeats) synced via the domain events raised here, exactly
/// as documented in that service's own Entities/Bus.cs.
/// </summary>
public sealed class Bus : AggregateRoot
{
    public Guid OperatorId { get; private set; }
    public string PlateNumber { get; private set; } = default!;
    public BusType BusType { get; private set; }
    public int TotalSeats { get; private set; }
    public Guid DepotId { get; private set; }
    public BusStatus Status { get; private set; }
    public string? Manufacturer { get; private set; }
    public string? Model { get; private set; }
    public int? YearOfManufacture { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private Bus() { } // EF Core

    private Bus(
        Guid id, Guid operatorId, string plateNumber, BusType busType, int totalSeats, Guid depotId,
        string? manufacturer, string? model, int? yearOfManufacture, DateTimeOffset now)
        : base(id)
    {
        OperatorId = operatorId;
        PlateNumber = plateNumber.Trim().ToUpperInvariant();
        BusType = busType;
        TotalSeats = totalSeats;
        DepotId = depotId;
        Status = BusStatus.Active;
        Manufacturer = manufacturer;
        Model = model;
        YearOfManufacture = yearOfManufacture;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public static Bus Register(
        Guid id, Guid operatorId, string plateNumber, BusType busType, int totalSeats, Guid depotId,
        string? manufacturer, string? model, int? yearOfManufacture, DateTimeOffset now)
    {
        var bus = new Bus(id, operatorId, plateNumber, busType, totalSeats, depotId, manufacturer, model, yearOfManufacture, now);
        bus.Raise(new BusRegisteredDomainEvent(bus.Id, bus.OperatorId, bus.PlateNumber, bus.BusType, bus.TotalSeats, bus.DepotId));
        return bus;
    }

    /// <summary>
    /// Updates the fields Booking Service's replica cares about, plus basic
    /// fleet details. Deliberately does NOT allow changing PlateNumber or
    /// OperatorId here — a plate reassignment or ownership transfer is a
    /// distinct, rarer operation with its own audit trail needs, not a
    /// routine detail edit; not built in this pass (see README, "Known
    /// gaps").
    /// </summary>
    public void UpdateDetails(BusType busType, int totalSeats, Guid depotId, string? manufacturer, string? model, int? yearOfManufacture, DateTimeOffset now)
    {
        BusType = busType;
        TotalSeats = totalSeats;
        DepotId = depotId;
        Manufacturer = manufacturer;
        Model = model;
        YearOfManufacture = yearOfManufacture;
        UpdatedAtUtc = now;

        Raise(new BusDetailsUpdatedDomainEvent(Id, PlateNumber, BusType, TotalSeats));
    }

    /// <summary>
    /// Status transitions: Active &lt;-&gt; UnderMaintenance freely; either can
    /// move to Retired; Retired is terminal (no transitions out). Retiring
    /// a bus is one-way by design — see docs/architecture, "Bus lifecycle"
    /// for why (a retired vehicle re-entering the fleet is modeled as a new
    /// registration, keeping the audit history of the retired one intact).
    /// </summary>
    public void ChangeStatus(BusStatus newStatus, DateTimeOffset now)
    {
        var isValidTransition = (Status, newStatus) switch
        {
            (BusStatus.Active, BusStatus.UnderMaintenance) => true,
            (BusStatus.Active, BusStatus.Retired) => true,
            (BusStatus.UnderMaintenance, BusStatus.Active) => true,
            (BusStatus.UnderMaintenance, BusStatus.Retired) => true,
            _ => false
        };

        if (!isValidTransition)
            throw new InvalidBusStatusTransitionException(Status, newStatus);

        var oldStatus = Status;
        Status = newStatus;
        UpdatedAtUtc = now;

        Raise(new BusStatusChangedDomainEvent(Id, oldStatus, newStatus));
    }
}
