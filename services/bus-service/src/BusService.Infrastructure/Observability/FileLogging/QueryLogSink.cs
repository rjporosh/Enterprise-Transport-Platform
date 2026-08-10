using System.Collections.Concurrent;

namespace BusService.Infrastructure.Observability.FileLogging;

/// <summary>
/// In-memory queue between the (hot-path, must-stay-fast) EF Core
/// interceptor and the (slow, does file I/O) background writer — enqueueing
/// here is an O(1), lock-free, non-blocking operation, so query logging
/// never adds measurable latency to an actual database call.
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
