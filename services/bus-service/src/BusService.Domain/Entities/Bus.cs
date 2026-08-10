using BusService.Domain.Common;
using BusService.Domain.Enums;
using BusService.Domain.Events;
using BusService.Domain.Exceptions;

namespace BusService.Domain.Entities;

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
    public Guid? TenantId { get; private set; }
    public Guid? CompanyId { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public new uint Version { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private Bus() { } // EF Core

    private Bus(
        Guid id, Guid operatorId, string plateNumber, BusType busType, int totalSeats, Guid depotId,
        string? manufacturer, string? model, int? yearOfManufacture, Guid? tenantId, Guid? companyId, Guid? organizationId, DateTimeOffset now)
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
        TenantId = tenantId;
        CompanyId = companyId;
        OrganizationId = organizationId;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public static Bus Register(
        Guid id, Guid operatorId, string plateNumber, BusType busType, int totalSeats, Guid depotId,
        string? manufacturer, string? model, int? yearOfManufacture, Guid? tenantId, Guid? companyId, Guid? organizationId, DateTimeOffset now)
    {
        var bus = new Bus(id, operatorId, plateNumber, busType, totalSeats, depotId, manufacturer, model, yearOfManufacture, tenantId, companyId, organizationId, now);
        bus.Raise(new BusRegisteredDomainEvent(bus.Id, bus.OperatorId, bus.PlateNumber, bus.BusType, bus.TotalSeats, bus.DepotId));
        return bus;
    }

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

    public void SoftDelete(string deletedBy, DateTimeOffset now)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAtUtc = now;
        DeletedBy = deletedBy;
        UpdatedAtUtc = now;
        Status = BusStatus.Retired;
        Raise(new BusSoftDeletedDomainEvent(Id, deletedBy));
    }

    public void Restore(DateTimeOffset now)
    {
        if (!IsDeleted) return;
        IsDeleted = false;
        DeletedAtUtc = null;
        DeletedBy = null;
        UpdatedAtUtc = now;
        Status = BusStatus.Active;
        Raise(new BusRestoredDomainEvent(Id));
    }
}
