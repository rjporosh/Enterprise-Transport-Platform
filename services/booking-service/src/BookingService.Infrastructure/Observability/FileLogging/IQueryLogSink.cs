namespace BookingService.Infrastructure.Observability.FileLogging;

public interface IQueryLogSink
{
    void Enqueue(QueryLogEntry entry);
}
