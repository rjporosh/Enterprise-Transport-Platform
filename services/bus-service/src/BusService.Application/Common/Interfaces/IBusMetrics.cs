namespace BusService.Application.Common.Interfaces;

public interface IBusMetrics
{
    void RecordBusRegistered();
    void RecordStatusChange(string newStatus);
}
