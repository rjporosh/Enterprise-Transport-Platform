using RouteService.Domain.Common;

namespace RouteService.Domain.Events;

public sealed class ScheduleCreatedDomainEvent : DomainEvent
{
    public Guid ScheduleId { get; }
    public Guid RouteId { get; }
    public TimeSpan DepartureTime { get; }
    public TimeSpan ArrivalTime { get; }
    public DateTimeOffset EffectiveFrom { get; }

    public ScheduleCreatedDomainEvent(Guid scheduleId, Guid routeId, TimeSpan departureTime, TimeSpan arrivalTime, DateTimeOffset effectiveFrom)
    {
        ScheduleId = scheduleId;
        RouteId = routeId;
        DepartureTime = departureTime;
        ArrivalTime = arrivalTime;
        EffectiveFrom = effectiveFrom;
    }
}
