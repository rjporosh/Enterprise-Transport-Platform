namespace PaymentService.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid? UserId { get; }
    string? TenantId { get; }
    Guid? CompanyId { get; }
    Guid? OrganizationId { get; }
    bool IsInRole(string role);
}
