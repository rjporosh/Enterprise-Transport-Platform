namespace Platform.SharedKernel.Tenancy;

/// <summary>
/// The tenant/company/organization the current operation runs on behalf of.
///
/// IMPORTANT (P0-11, .ai/MASTER-RULES.md §25): a tenant context is only ever
/// built from a TRUSTED source — a validated JWT claim or a secure server-side
/// resolver — never from a raw client-supplied <c>X-Tenant-Id</c> header. The
/// gateway strips client-supplied tenant headers at the edge; services adopt
/// claim-based resolution in later milestones (M1/M3).
/// </summary>
public sealed record TenantContext
{
    /// <summary>Context used before authentication has resolved a tenant (e.g. anonymous endpoints).</summary>
    public static readonly TenantContext None = new();

    private TenantContext() { }

    public TenantContext(Guid tenantId, Guid? companyId = null, Guid? organizationId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId must not be empty.", nameof(tenantId));

        TenantId = tenantId;
        CompanyId = companyId;
        OrganizationId = organizationId;
    }

    public Guid? TenantId { get; }
    public Guid? CompanyId { get; }
    public Guid? OrganizationId { get; }

    /// <summary>True when a tenant has actually been resolved from a trusted source.</summary>
    public bool IsResolved => TenantId is { } t && t != Guid.Empty;

    public Guid RequireTenantId() =>
        TenantId is { } t && t != Guid.Empty
            ? t
            : throw new InvalidOperationException(
                "No tenant context has been resolved for this operation. " +
                "This request must be authenticated and carry a tenant claim.");
}

/// <summary>
/// Accessor for the current <see cref="TenantContext"/>. Registered per-scope by
/// each service once it adopts claim-based tenant resolution.
/// </summary>
public interface ITenantContextAccessor
{
    TenantContext Current { get; }
}
