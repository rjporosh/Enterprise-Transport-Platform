using System.Collections.Concurrent;

namespace BookingService.Infrastructure.Observability.FileLogging;

/// <summary>
/// Lock-free, non-blocking queue between the hot-path EF Core interceptor
/// and the slow, file-I/O-bound background writer. Enqueue is O(1) so query
/// logging adds no measurable latency to an actual database call.
/// </summary>
public sealed class QueryLogSink : IQueryLogSink
{
    private readonly ConcurrentQueue<QueryLogEntry> _queue = new();

    public void Enqueue(QueryLogEntry entry) => _queue.Enqueue(entry);

    public IReadOnlyCollection<QueryLogEntry> DrainAll()
    {
        var drained = new List<QueryLogEntry>();
        while (_queue.TryDequeue(out var entry))
            drained.Add(entry);
        return drained;
    }
}
