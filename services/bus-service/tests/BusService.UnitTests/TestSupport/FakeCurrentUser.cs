using BusService.Application.Common.Interfaces;

namespace BusService.UnitTests.TestSupport;

public sealed class FakeCurrentUser : ICurrentUser
{
    public Guid? UserId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? OrganizationId { get; set; }

    public bool IsInRole(string role) => false;
}
