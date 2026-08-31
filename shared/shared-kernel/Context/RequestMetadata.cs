using Platform.SharedKernel.Tenancy;

namespace Platform.SharedKernel.Context;

/// <summary>
/// Immutable snapshot of the ambient cross-cutting context for one inbound
/// operation (HTTP request, consumed message, or scheduled job run). Assembled
/// at the edge from TRUSTED sources only and passed down explicitly or via a
/// scoped accessor — never reconstructed from raw client input downstream.
/// </summary>
public sealed record RequestMetadata
{
    public required string CorrelationId { get; init; }

    /// <summary>W3C trace id when tracing is active; otherwise <c>null</c>.</summary>
    public string? TraceId { get; init; }

    public TenantContext Tenant { get; init; } = TenantContext.None;

    /// <summary>Authenticated subject (user) id, when the request is authenticated.</summary>
    public Guid? UserId { get; init; }

    /// <summary>Client IP as resolved through the trusted proxy chain (never a raw header value).</summary>
    public string? ClientIp { get; init; }

    public string? UserAgent { get; init; }

    /// <summary><c>Idempotency-Key</c> header value, when supplied.</summary>
    public string? IdempotencyKey { get; init; }

    /// <summary>Origin surface — "gateway", "grpc", "message-consumer", "job", etc.</summary>
    public string Source { get; init; } = "unknown";
}
