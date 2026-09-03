namespace PaymentService.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid? UserId { get; }

    /// <summary>The customer id — <c>customer_id</c> claim, falling back to <see cref="UserId"/>.</summary>
    Guid? CustomerId { get; }

    string? TenantId { get; }
    Guid? CompanyId { get; }
    Guid? OrganizationId { get; }
    bool IsInRole(string role);
}
