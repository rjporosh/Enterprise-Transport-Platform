namespace Platform.SharedKernel.Correlation;

/// <summary>
/// Canonical HTTP header names used for cross-cutting context propagation
/// across the platform (gateway, HTTP services, and — where carried — RabbitMQ
/// message headers). Every component must use these exact spellings so a
/// request stays traceable end to end.
/// </summary>
public static class PlatformHeaders
{
    /// <summary>Business correlation id — one logical operation, potentially many spans/services.</summary>
    public const string CorrelationId = "X-Correlation-Id";

    /// <summary>W3C trace context (handled by OpenTelemetry; listed here so proxies never strip it).</summary>
    public const string TraceParent = "traceparent";

    /// <summary>
    /// Tenant id. TRUSTED ONLY when set by the gateway from a validated JWT claim.
    /// The gateway strips any client-supplied value before forwarding (see
    /// docs/programmers-guide/correlation-id.md and P0-11).
    /// </summary>
    public const string TenantId = "X-Tenant-Id";

    /// <summary>Company id (SaaS hierarchy). Same trust rules as <see cref="TenantId"/>.</summary>
    public const string CompanyId = "X-Company-Id";

    /// <summary>Organization id (SaaS hierarchy). Same trust rules as <see cref="TenantId"/>.</summary>
    public const string OrganizationId = "X-Organization-Id";

    /// <summary>Idempotency key for safely-retryable state-changing requests.</summary>
    public const string IdempotencyKey = "Idempotency-Key";

    /// <summary>Marks a request as having passed through the platform gateway.</summary>
    public const string ForwardedByGateway = "X-Forwarded-By-Gateway";
}
