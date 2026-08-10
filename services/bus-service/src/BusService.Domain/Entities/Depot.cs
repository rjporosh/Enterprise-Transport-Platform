using BusService.Domain.Common;

namespace BusService.Domain.Entities;

public sealed class Depot : Entity
{
    public string Name { get; private set; } = default!;
    public string City { get; private set; } = default!;
    public string? Address { get; private set; }
    public Guid? TenantId { get; private set; }
    public Guid? CompanyId { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private Depot() { } // EF Core

    private Depot(Guid id, string name, string city, string? address, Guid? tenantId, Guid? companyId, Guid? organizationId) : base(id)
    {
        Name = name;
        City = city;
        Address = address;
        TenantId = tenantId;
        CompanyId = companyId;
        OrganizationId = organizationId;
    }

    public static Depot Create(Guid id, string name, string city, string? address, Guid? tenantId = null, Guid? companyId = null, Guid? organizationId = null) =>
        new(id, name, city, address, tenantId, companyId, organizationId);

    public void SoftDelete(string deletedBy, DateTimeOffset now)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAtUtc = now;
        DeletedBy = deletedBy;
    }

    public void Restore(DateTimeOffset now)
    {
        if (!IsDeleted) return;
        IsDeleted = false;
        DeletedAtUtc = null;
        DeletedBy = null;
    }
}
