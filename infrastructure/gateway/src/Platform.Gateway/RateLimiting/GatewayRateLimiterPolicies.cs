using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Platform.SharedKernel.Correlation;

namespace Platform.Gateway.RateLimiting;

/// <summary>
/// Gateway rate-limiting. Partitions by, in order of preference:
///   tenant id (claim) → user id (claim) → client IP.
///
/// This is a DoS / abuse backstop at the edge, not the plan-aware per-tenant
/// quota system (that is ADR-0009 / milestone M10). It is deliberately built on
/// the partition abstraction the platform will keep — only the STORE changes in
/// M9 (in-memory fixed-window here → Redis-backed sliding window), not the
/// partition keys or the policy names.
///
/// Not MAC-address based: browsers cannot expose a MAC, and the audit
/// explicitly rules it out.
/// </summary>
public static class GatewayRateLimiterPolicies
{
    public const string Global = "gateway-global";
    public const string Auth = "gateway-auth";
    public const string Payment = "gateway-payment";

    public static void AddGatewayRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection("Gateway:RateLimiting").Get<GatewayRateLimitOptions>()
                      ?? new GatewayRateLimitOptions();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiter.OnRejected = static async (context, token) =>
            {
                var correlationId = context.HttpContext.Items[Platform.Common.Correlation.CorrelationIdMiddleware.HttpContextItemKey] as string
                                    ?? CorrelationContext.Current
                                    ?? "unknown";

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsync(
                    $$"""
                    {"success":false,"message":"Too many requests. Please retry later.","errors":[{"code":"rate_limit.exceeded","message":"Rate limit exceeded at the API gateway.","field":null}],"traceId":"{{correlationId}}"}
                    """,
                    token);
            };

            limiter.AddPolicy(Global, ctx => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ResolvePartitionKey(ctx, "global"),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.GlobalPermitLimit,
                    Window = TimeSpan.FromSeconds(options.WindowSeconds),
                    QueueLimit = 0
                }));

            limiter.AddPolicy(Auth, ctx => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ResolvePartitionKey(ctx, "auth"),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.AuthPermitLimit,
                    Window = TimeSpan.FromSeconds(options.WindowSeconds),
                    QueueLimit = 0
                }));

            limiter.AddPolicy(Payment, ctx => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ResolvePartitionKey(ctx, "payment"),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.PaymentPermitLimit,
                    Window = TimeSpan.FromSeconds(options.WindowSeconds),
                    QueueLimit = 0
                }));
        });
    }

    /// <summary>tenant &gt; user &gt; ip, prefixed by the policy bucket so buckets don't share a budget.</summary>
    private static string ResolvePartitionKey(HttpContext context, string bucket)
    {
        var user = context.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            var tenant = user.FindFirst("tenant_id")?.Value;
            if (!string.IsNullOrWhiteSpace(tenant))
                return $"{bucket}:t:{tenant}";

            var subject = user.FindFirst("sub")?.Value
                          ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(subject))
                return $"{bucket}:u:{subject}";
        }

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"{bucket}:ip:{ip}";
    }
}

public sealed class GatewayRateLimitOptions
{
    public int WindowSeconds { get; set; } = 60;
    public int GlobalPermitLimit { get; set; } = 300;
    public int AuthPermitLimit { get; set; } = 20;
    public int PaymentPermitLimit { get; set; } = 60;
}
