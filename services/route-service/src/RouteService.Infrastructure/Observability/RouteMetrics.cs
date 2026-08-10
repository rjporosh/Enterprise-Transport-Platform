using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics;
using RouteService.Application.Common.Interfaces;

namespace RouteService.Infrastructure.Observability;

public sealed class RouteMetrics : IRouteMetrics
{
    public const string MeterName = "RouteService";

    private readonly Counter<long> _routesCreated;
    private readonly Counter<long> _routesDeleted;
    private readonly Counter<long> _schedulesCreated;

    public RouteMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _routesCreated = meter.CreateCounter<long>("routes_created_total", unit: "{route}", description: "Total routes created.");
        _routesDeleted = meter.CreateCounter<long>("routes_deleted_total", unit: "{route}", description: "Total routes soft-deleted.");
        _schedulesCreated = meter.CreateCounter<long>("schedules_created_total", unit: "{schedule}", description: "Total schedules created.");
    }

    public void RecordRouteCreated() => _routesCreated.Add(1);
    public void RecordRouteDeleted() => _routesDeleted.Add(1);
    public void RecordScheduleCreated() => _schedulesCreated.Add(1);
}
