using RouteService.Domain.Common;
using RouteService.Domain.Interfaces;
using RouteService.Domain.Events;
using RouteService.Domain.Enums;
using RouteService.Domain.Exceptions;

namespace RouteService.Domain.Entities;

public sealed class Schedule : AggregateRoot, IAuditable
{
    public Guid RouteId { get; private set; }
    public Route? Route { get; set; }

    public TimeSpan DepartureTime { get; private set; }
    public TimeSpan ArrivalTime { get; private set; }
    public ScheduleStatus Status { get; private set; }
    public DateTimeOffset EffectiveFrom { get; private set; }
    public DateTimeOffset? EffectiveTo { get; private set; }

    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }

    private Schedule() { }

    private Schedule(Guid id, Guid routeId, TimeSpan departureTime, TimeSpan arrivalTime, ScheduleStatus status, DateTimeOffset effectiveFrom, DateTimeOffset? effectiveTo, DateTimeOffset now)
        : base(id)
    {
        RouteId = routeId;
        DepartureTime = departureTime;
        ArrivalTime = arrivalTime;
        Status = status;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
        IsDeleted = false;
        Version = 1;
    }

    public static Schedule Create(Guid id, Guid routeId, TimeSpan departureTime, TimeSpan arrivalTime, DateTimeOffset effectiveFrom, DateTimeOffset? effectiveTo, DateTimeOffset now)
    {
        if (arrivalTime <= departureTime)
            throw new InvalidScheduleException("Arrival time must be after departure time.");

        var schedule = new Schedule(id, routeId, departureTime, arrivalTime, ScheduleStatus.Planned, effectiveFrom, effectiveTo, now);
        schedule.Raise(new ScheduleCreatedDomainEvent(schedule.Id, schedule.RouteId, schedule.DepartureTime, schedule.ArrivalTime, schedule.EffectiveFrom));
        return schedule;
    }

    public void Update(TimeSpan departureTime, TimeSpan arrivalTime, DateTimeOffset? effectiveTo, DateTimeOffset now)
    {
        if (Status == ScheduleStatus.Completed)
            throw new InvalidScheduleException("Cannot update a completed schedule.");

        if (arrivalTime <= departureTime)
            throw new InvalidScheduleException("Arrival time must be after departure time.");

        DepartureTime = departureTime;
        ArrivalTime = arrivalTime;
        EffectiveTo = effectiveTo;
        UpdatedAtUtc = now;
        Version++;
    }

    public void Activate(DateTimeOffset now)
    {
        if (Status != ScheduleStatus.Planned)
            throw new InvalidScheduleException("Only planned schedules can be activated.");

        Status = ScheduleStatus.Active;
        UpdatedAtUtc = now;
        Version++;
    }

    public void Suspend(DateTimeOffset now)
    {
        if (Status != ScheduleStatus.Active)
            throw new InvalidScheduleException("Only active schedules can be suspended.");

        Status = ScheduleStatus.Suspended;
        UpdatedAtUtc = now;
        Version++;
    }

    public void Complete(DateTimeOffset now)
    {
        if (Status != ScheduleStatus.Active)
            throw new InvalidScheduleException("Only active schedules can be completed.");

        Status = ScheduleStatus.Completed;
        UpdatedAtUtc = now;
        Version++;
    }

    public void SoftDelete(DateTimeOffset now)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void Restore(DateTimeOffset now)
    {
        if (!IsDeleted) return;
        IsDeleted = false;
        DeletedAtUtc = null;
        UpdatedAtUtc = now;
        Status = ScheduleStatus.Planned;
    }
}
