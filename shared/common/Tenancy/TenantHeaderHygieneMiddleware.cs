using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Platform.SharedKernel.Correlation;

namespace Platform.Common.Tenancy;

/// <summary>
/// Tenant-context header hygiene at the trust boundary (P0-11,
/// .ai/MASTER-RULES.md §25).
///
/// 1. UNCONDITIONALLY strips any client-supplied <c>X-Tenant-Id</c> /
///    <c>X-Company-Id</c> / <c>X-Organization-Id</c> from the inbound request —
///    a browser or API client must never be able to assert a tenant.
/// 2. If the request carries a validated identity with a tenant claim, RE-ADDS
///    those headers from the claim values, so downstream services receive a
///    trustworthy tenant context on the header they already read.
///
/// Runs at the gateway (after authentication, before proxying). Downstream
/// services keep reading the same header name — but now its presence means "the
/// gateway vouched for this", not "the client asked for this".
/// </summary>
public sealed class TenantHeaderHygieneMiddleware(RequestDelegate next, ILogger<TenantHeaderHygieneMiddleware> logger)
{
    /// <summary>JWT claim types the tenant/company/org values are read from (M1 adds these to issued tokens).</summary>
    public const string TenantClaim = "tenant_id";
    public const string CompanyClaim = "company_id";
    public const string OrganizationClaim = "organization_id";

    private static readonly string[] TenantHeaders =
    [
        PlatformHeaders.TenantId,
        PlatformHeaders.CompanyId,
        PlatformHeaders.OrganizationId
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        var hadClientTenantHeader = false;
        foreach (var header in TenantHeaders)
        {
            if (context.Request.Headers.Remove(header))
                hadClientTenantHeader = true;
        }

        if (hadClientTenantHeader)
        {
            logger.LogWarning(
                "Stripped client-supplied tenant header(s) from {Path}. Tenant context is only ever set from a validated claim.",
                context.Request.Path);
        }

        if (context.User.Identity?.IsAuthenticated == true)
        {
            SetFromClaim(context, TenantClaim, PlatformHeaders.TenantId);
            SetFromClaim(context, CompanyClaim, PlatformHeaders.CompanyId);
            SetFromClaim(context, OrganizationClaim, PlatformHeaders.OrganizationId);
        }

        await next(context);
    }

    private static void SetFromClaim(HttpContext context, string claimType, string headerName)
    {
        var value = context.User.FindFirstValue(claimType);
        if (!string.IsNullOrWhiteSpace(value))
            context.Request.Headers[headerName] = value;
    }
}
