using Microsoft.AspNetCore.Builder;
using Platform.Common.Correlation;
using Platform.Common.Security;
using Platform.Common.Tenancy;

namespace Platform.Common.DependencyInjection;

/// <summary>Pipeline helpers so a host wires the platform middleware in the correct order.</summary>
public static class PlatformCommonExtensions
{
    /// <summary>
    /// Correlation id first (so every later log line — including exception logs —
    /// carries it), then security headers.
    /// </summary>
    public static IApplicationBuilder UsePlatformEdge(this IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        return app;
    }

    /// <summary>
    /// Tenant-header hygiene. MUST run after authentication (needs the validated
    /// principal) and before the request is proxied/handled.
    /// </summary>
    public static IApplicationBuilder UseTenantHeaderHygiene(this IApplicationBuilder app)
    {
        app.UseMiddleware<TenantHeaderHygieneMiddleware>();
        return app;
    }
}
