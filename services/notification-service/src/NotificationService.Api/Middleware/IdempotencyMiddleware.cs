using System.Collections.Concurrent;

namespace NotificationService.Api.Middleware;

/// <summary>
/// Supports an Idempotency-Key header (CLAUDE.md, "Support Idempotency
/// Key") on POST/PUT/PATCH: if a request with the same key was already
/// completed, the cached response is replayed instead of re-executing the
/// handler -- prevents double-sends when a caller retries a timed-out
/// SendNotification call. In-memory ConcurrentDictionary is a deliberate,
/// documented scope limitation for a single-instance/demo deployment; a
/// real multi-replica deployment needs this backed by Redis (the same
/// store AuthService already uses for caching) so idempotency holds across
/// instances -- see Known Limitations in this delivery's final report and
/// docs/programmers-guide/troubleshooting.md.
/// </summary>
public sealed class IdempotencyMiddleware
{
    private const string HeaderName = "Idempotency-Key";
    private static readonly TimeSpan EntryLifetime = TimeSpan.FromHours(24);

    private sealed record CachedResponse(int StatusCode, string ContentType, byte[] Body, DateTimeOffset ExpiresAtUtc);

    private static readonly ConcurrentDictionary<string, CachedResponse> Cache = new();

    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    public IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var isMutating = HttpMethods.IsPost(context.Request.Method) || HttpMethods.IsPut(context.Request.Method) || HttpMethods.IsPatch(context.Request.Method);
        if (!isMutating || !context.Request.Headers.TryGetValue(HeaderName, out var keyValues))
        {
            await _next(context);
            return;
        }

        var key = keyValues.ToString();
        if (string.IsNullOrWhiteSpace(key))
        {
            await _next(context);
            return;
        }

        if (Cache.TryGetValue(key, out var cached) && cached.ExpiresAtUtc > DateTimeOffset.UtcNow)
        {
            _logger.LogInformation("Replaying cached response for Idempotency-Key {IdempotencyKey}.", key);
            context.Response.StatusCode = cached.StatusCode;
            context.Response.ContentType = cached.ContentType;
            context.Response.Headers["Idempotency-Replayed"] = "true";
            await context.Response.Body.WriteAsync(cached.Body);
            return;
        }

        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        await _next(context);

        buffer.Seek(0, SeekOrigin.Begin);
        var bodyBytes = buffer.ToArray();

        if (context.Response.StatusCode is >= 200 and < 300)
        {
            Cache[key] = new CachedResponse(context.Response.StatusCode, context.Response.ContentType ?? "application/json", bodyBytes, DateTimeOffset.UtcNow.Add(EntryLifetime));
        }

        await originalBody.WriteAsync(bodyBytes);
        context.Response.Body = originalBody;
    }
}
