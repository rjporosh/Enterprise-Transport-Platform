using BookingService.Infrastructure.Observability.FileLogging;
using Platform.SharedKernel.Correlation;

namespace BookingService.Api.Middleware;

/// <summary>
/// Ensures every request/response carries an <c>X-Correlation-Id</c>,
/// generating one if the caller (or the gateway) didn't supply it. Pushes it
/// into Serilog's <c>LogContext</c>, the platform's ambient
/// <see cref="CorrelationContext"/>, and the query-log
/// <see cref="CurrentRequestContext"/> (alongside the endpoint name) so every
/// log line and every logged SQL statement for this request is correlatable.
/// </summary>
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
        context.Items[HeaderName] = correlationId;

        var endpointName = context.GetEndpoint()?.DisplayName ?? $"{context.Request.Method} {context.Request.Path}";
        CurrentRequestContext.SetEndpoint(endpointName);
        CurrentRequestContext.SetCorrelationId(correlationId);

        using (CorrelationContext.BeginScope(correlationId))
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            try
            {
                await _next(context);
            }
            finally
            {
                CurrentRequestContext.SetEndpoint(null);
                CurrentRequestContext.SetHandler(null);
                CurrentRequestContext.SetCorrelationId(null);
            }
        }
    }
}
