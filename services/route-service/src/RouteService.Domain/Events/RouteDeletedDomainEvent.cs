using RouteService.Domain.Common;

namespace RouteService.Domain.Events;

public sealed class RouteDeletedDomainEvent : DomainEvent
{
    public Guid RouteId { get; }
    public string Code { get; }

    public RouteDeletedDomainEvent(Guid routeId, string code)
    {
        RouteId = routeId;
        Code = code;
    }
}
