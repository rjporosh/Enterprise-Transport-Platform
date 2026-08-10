namespace RouteService.Api.Middleware;

public sealed class RequestContextMiddleware
{
    private const string HeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public RequestContextMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing) && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString();

        RouteService.Infrastructure.Observability.FileLogging.CurrentRequestContext.CorrelationId = correlationId;

        await _next(context);
    }
}
