using BusService.Infrastructure.Observability.FileLogging;

namespace BusService.Api.Middleware;

/// <summary>
/// Sets CurrentRequestContext.Endpoint at the start of every request so the
/// EF Core QueryLoggingInterceptor (Infrastructure layer) can tag every SQL
/// statement with the endpoint that triggered it, without Infrastructure
/// needing any ASP.NET Core dependency — see that class' doc comment.
/// </summary>
public sealed class RequestContextMiddleware
{
    private readonly RequestDelegate _next;

    public RequestContextMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        CurrentRequestContext.SetEndpoint($"{context.Request.Method} {context.Request.Path}");
        await _next(context);
    }
}
