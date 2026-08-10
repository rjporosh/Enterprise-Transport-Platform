using RouteService.Domain.Common;

namespace RouteService.Domain.Events;

public sealed class RouteStatusChangedDomainEvent : DomainEvent
{
    public Guid RouteId { get; }
    public string Code { get; }
    public RouteService.Domain.Enums.RouteStatus OldStatus { get; }
    public RouteService.Domain.Enums.RouteStatus NewStatus { get; }

    public RouteStatusChangedDomainEvent(Guid routeId, string code, RouteService.Domain.Enums.RouteStatus oldStatus, RouteService.Domain.Enums.RouteStatus newStatus)
    {
        RouteId = routeId;
        Code = code;
        OldStatus = oldStatus;
        NewStatus = newStatus;
    }
}
