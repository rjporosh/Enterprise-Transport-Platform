using RouteService.Domain.Common;

namespace RouteService.Domain.Events;

public sealed class RouteUpdatedDomainEvent : DomainEvent
{
    public Guid RouteId { get; }
    public string Code { get; }
    public string Name { get; }
    public RouteService.Domain.Enums.TransportMode TransportMode { get; }

    public RouteUpdatedDomainEvent(Guid routeId, string code, string name, RouteService.Domain.Enums.TransportMode transportMode)
    {
        RouteId = routeId;
        Code = code;
        Name = name;
        TransportMode = transportMode;
    }
}
