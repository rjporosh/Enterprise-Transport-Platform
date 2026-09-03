namespace BookingService.Application.Common.Interfaces;

/// <summary>
/// Identity of the caller, populated from the validated JWT by the API layer.
/// Kept as an interface so handlers never touch HttpContext directly.
/// </summary>
public interface ICurrentUser
{
    /// <summary>The authenticated principal's user id (<c>sub</c> / nameidentifier). Null when unauthenticated.</summary>
    Guid? UserId { get; }

    /// <summary>The customer id — equals <see cref="UserId"/> for a customer principal; from the <c>customer_id</c> claim otherwise.</summary>
    Guid? CustomerId { get; }

    string? TenantId { get; }
    string? Email { get; }
    string? FullName { get; }
    string? PhoneNumber { get; }

    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
