namespace Platform.SharedKernel.Correlation;

/// <summary>
/// Ambient correlation id for the current logical operation, flowing with the
/// async execution context (so it survives <c>await</c>, <see cref="Task.Run(Action)"/>,
/// and background continuations within one operation).
///
/// Set once at the edge (HTTP middleware, message consumer, or job runner) via
/// <see cref="BeginScope"/>; read anywhere downstream (e.g. an outbound HTTP
/// handler or a RabbitMQ publisher) via <see cref="Current"/>.
///
/// This replaces the per-service <c>static string CurrentRequestContext.CorrelationId</c>
/// fields flagged in the audit (P1-16) as racy under concurrency — an
/// <see cref="AsyncLocal{T}"/> is isolated per async flow, a plain static is not.
/// </summary>
public static class CorrelationContext
{
    private static readonly AsyncLocal<string?> _current = new();

    /// <summary>The correlation id for the current async flow, or <c>null</c> if none was set.</summary>
    public static string? Current => _current.Value;

    /// <summary>
    /// Establishes <paramref name="correlationId"/> as the ambient value for the
    /// current async flow and every continuation spawned from it. Dispose the
    /// returned scope to restore the previous value.
    /// </summary>
    public static IDisposable BeginScope(string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        var previous = _current.Value;
        _current.Value = correlationId;
        return new Scope(previous);
    }

    private sealed class Scope(string? previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _current.Value = previous;
        }
    }
}
