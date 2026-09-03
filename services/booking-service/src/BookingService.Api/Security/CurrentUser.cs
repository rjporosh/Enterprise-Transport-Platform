using System.Security.Claims;
using BookingService.Application.Common.Interfaces;

namespace BookingService.Api.Security;

/// <summary>
/// Reads the caller's identity from the validated JWT. The
/// <c>JwtSecurityTokenHandler</c> maps <c>sub</c> → <see cref="ClaimTypes.NameIdentifier"/>
/// and <c>email</c> → <see cref="ClaimTypes.Email"/> by default; custom claims
/// (<c>customer_id</c>, <c>tenant_id</c>, <c>phone_number</c>, name parts) are
/// read verbatim.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId => ParseGuid(
        Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Principal?.FindFirstValue("sub"));

    public Guid? CustomerId => ParseGuid(Principal?.FindFirstValue("customer_id")) ?? UserId;

    public string? TenantId => Principal?.FindFirstValue("tenant_id");

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email) ?? Principal?.FindFirstValue("email");

    public string? FullName
    {
        get
        {
            var first = Principal?.FindFirstValue("first_name");
            var last = Principal?.FindFirstValue("last_name");
            var joined = string.Join(' ', new[] { first, last }.Where(s => !string.IsNullOrWhiteSpace(s)));
            return string.IsNullOrWhiteSpace(joined) ? Principal?.FindFirstValue(ClaimTypes.Name) : joined;
        }
    }

    public string? PhoneNumber => Principal?.FindFirstValue("phone_number");

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;

    private static Guid? ParseGuid(string? value) => Guid.TryParse(value, out var id) ? id : null;
}
