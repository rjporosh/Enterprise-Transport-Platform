using RouteService.Domain.Common;

namespace RouteService.Domain.Events;

public sealed class RouteCreatedDomainEvent : DomainEvent
{
    public Guid RouteId { get; }
    public string Code { get; }
    public string Name { get; }
    public Guid OriginStopId { get; }
    public Guid DestinationStopId { get; }
    public RouteService.Domain.Enums.TransportMode TransportMode { get; }

    public RouteCreatedDomainEvent(Guid routeId, string code, string name, Guid originStopId, Guid destinationStopId, RouteService.Domain.Enums.TransportMode transportMode)
    {
        RouteId = routeId;
        Code = code;
        Name = name;
        OriginStopId = originStopId;
        DestinationStopId = destinationStopId;
        TransportMode = transportMode;
    }
}
