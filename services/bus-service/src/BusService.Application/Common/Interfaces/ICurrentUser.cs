namespace BusService.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid? UserId { get; }
    Guid? TenantId { get; }
    Guid? CompanyId { get; }
    Guid? OrganizationId { get; }
    bool IsInRole(string role);
}
