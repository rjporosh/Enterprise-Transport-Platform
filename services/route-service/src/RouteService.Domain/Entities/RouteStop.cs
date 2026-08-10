using RouteService.Domain.Common;

namespace RouteService.Domain.Entities;

public sealed class RouteStop : Entity
{
    public Guid RouteId { get; set; }
    public Route? Route { get; set; }

    public Guid StopId { get; set; }
    public Stop? Stop { get; set; }

    public int StopOrder { get; set; }
    public TimeSpan? ArrivalTimeOffset { get; set; }
    public TimeSpan? DepartureTimeOffset { get; set; }

    private RouteStop() { }

    public RouteStop(Guid id, Guid routeId, Guid stopId, int stopOrder, TimeSpan? arrivalTimeOffset, TimeSpan? departureTimeOffset)
        : base(id)
    {
        RouteId = routeId;
        StopId = stopId;
        StopOrder = stopOrder;
        ArrivalTimeOffset = arrivalTimeOffset;
        DepartureTimeOffset = departureTimeOffset;
    }
}
