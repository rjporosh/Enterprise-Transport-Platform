namespace RouteService.Application.Common.Interfaces;

public interface IRouteMetrics
{
    void RecordRouteCreated();
    void RecordRouteDeleted();
    void RecordScheduleCreated();
}
