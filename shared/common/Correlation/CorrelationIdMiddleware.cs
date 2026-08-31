using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Platform.SharedKernel.Correlation;

namespace Platform.Common.Correlation;

/// <summary>
/// Canonical correlation-id middleware for the platform.
///
/// * Reads <see cref="PlatformHeaders.CorrelationId"/> from the request.
/// * If absent or malformed, generates a fresh one (never trusts arbitrary
///   values — .ai/MASTER-RULES.md §39).
/// * Publishes it to <see cref="CorrelationContext"/> (AsyncLocal) for the whole
///   request, so outbound HTTP handlers / publishers pick it up automatically.
/// * Pushes it into the logging scope as <c>CorrelationId</c>.
/// * Echoes it on the response and stores it in <see cref="HttpContext.Items"/>.
///
/// Register FIRST in the pipeline (before exception handling) so every log line
/// — including unhandled-exception logs — carries it. (The audit found
/// booking-service and payment-service had this ordering wrong; the gateway and
/// any service adopting this middleware get it right.)
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    /// <summary>Key under which the id is stored in <see cref="HttpContext.Items"/>.</summary>
    public const string HttpContextItemKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers.TryGetValue(PlatformHeaders.CorrelationId, out var header)
            ? header.ToString()
            : null;

        var correlationId = CorrelationId.NormalizeOrCreate(incoming);
        var wasGenerated = !CorrelationId.IsValid(incoming);

        context.Items[HttpContextItemKey] = correlationId;
        context.Request.Headers[PlatformHeaders.CorrelationId] = correlationId;

        context.Response.OnStarting(static state =>
        {
            var ctx = (HttpContext)state;
            var id = (string)ctx.Items[HttpContextItemKey]!;
            ctx.Response.Headers[PlatformHeaders.CorrelationId] = new StringValues(id);
            return Task.CompletedTask;
        }, context);

        using var _ = CorrelationContext.BeginScope(correlationId);
        using var __ = logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["CorrelationIdGenerated"] = wasGenerated
        });

        await next(context);
    }
}
