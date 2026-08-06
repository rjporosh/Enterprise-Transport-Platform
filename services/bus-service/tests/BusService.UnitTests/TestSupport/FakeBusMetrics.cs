using BusService.Application.Common.Interfaces;

namespace BusService.UnitTests.TestSupport;

public sealed class FakeBusMetrics : IBusMetrics
{
    public int RegisteredCount { get; private set; }
    public List<string> StatusChanges { get; } = new();

    public void RecordBusRegistered() => RegisteredCount++;
    public void RecordStatusChange(string newStatus) => StatusChanges.Add(newStatus);
}
