using System.Diagnostics.Metrics;
using BusService.Application.Common.Interfaces;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace BusService.Infrastructure.Observability;

public sealed class BusMetrics : IBusMetrics
{
    public const string MeterName = "BusService";

    private readonly Counter<long> _busesRegistered;
    private readonly Counter<long> _statusChanges;

    public BusMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _busesRegistered = meter.CreateCounter<long>("bus_registrations_total", unit: "{bus}", description: "Number of buses registered into the fleet.");
        _statusChanges = meter.CreateCounter<long>("bus_status_changes_total", unit: "{change}", description: "Number of bus status transitions, tagged by the resulting status.");
    }

    public void RecordBusRegistered() => _busesRegistered.Add(1);
    public void RecordStatusChange(string newStatus) => _statusChanges.Add(1, new KeyValuePair<string, object?>("status", newStatus));
}
