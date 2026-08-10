using RouteService.Domain.Common;

namespace RouteService.Domain.Events;

public sealed class StopCreatedDomainEvent : DomainEvent
{
    public Guid StopId { get; }
    public string Code { get; }
    public string Name { get; }
    public string City { get; }

    public StopCreatedDomainEvent(Guid stopId, string code, string name, string city)
    {
        StopId = stopId;
        Code = code;
        Name = name;
        City = city;
    }
}
