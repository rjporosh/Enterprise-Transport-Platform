using RouteService.Application.Common.Interfaces;

namespace RouteService.UnitTests.TestSupport;

public sealed class FakeRouteMetrics : IRouteMetrics
{
    public int RoutesCreated { get; private set; }
    public int RoutesDeleted { get; private set; }
    public int SchedulesCreated { get; private set; }

    public void RecordRouteCreated() => RoutesCreated++;
    public void RecordRouteDeleted() => RoutesDeleted++;
    public void RecordScheduleCreated() => SchedulesCreated++;
}
