using RouteService.Domain.Common;

namespace RouteService.Domain.Events;

public sealed class StopUpdatedDomainEvent : DomainEvent
{
    public Guid StopId { get; }
    public string Code { get; }
    public string Name { get; }

    public StopUpdatedDomainEvent(Guid stopId, string code, string name)
    {
        StopId = stopId;
        Code = code;
        Name = name;
    }
}
