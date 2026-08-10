using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BusService.Api.Middleware;

public sealed class IpTracingMiddleware
{
    private const string HeaderName = "X-Forwarded-For";
    private readonly RequestDelegate _next;
    private readonly ILogger<IpTracingMiddleware> _logger;

    public IpTracingMiddleware(RequestDelegate next, ILogger<IpTracingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        var forwardedIp = context.Request.Headers.TryGetValue(HeaderName, out var forwarded) && !string.IsNullOrWhiteSpace(forwarded)
            ? forwarded.ToString().Split(',').FirstOrDefault()?.Trim()
            : null;

        var clientIp = forwardedIp ?? remoteIp;

        if (!string.IsNullOrEmpty(clientIp))
        {
            context.Items["ClientIp"] = clientIp;
            using (Serilog.Context.LogContext.PushProperty("ClientIp", clientIp))
            {
                await _next(context);
            }
        }
        else
        {
            await _next(context);
        }
    }
}
