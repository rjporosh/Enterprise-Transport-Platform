namespace NotificationService.Api.Middleware;

/// <summary>Ensures every request/response carries an X-Correlation-Id and pushes it into Serilog's LogContext -- identical contract to every other service (Booking/Auth) so a correlation id survives a full cross-service trace through the log aggregator (Seq/Grafana Loki/ELK/Graylog -- see docs/programmers-guide/observability.md).</summary>
public sealed class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing) && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString();

        context.Response.Headers[HeaderName] = correlationId;
        context.TraceIdentifier = correlationId;

        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
