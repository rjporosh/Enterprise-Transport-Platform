namespace BookingService.Application.Common.Interfaces;

/// <summary>
/// Identity of the caller, populated from the validated JWT by the API layer.
/// Kept as an interface so handlers never touch HttpContext directly.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? TenantId { get; }
    bool IsInRole(string role);
}
