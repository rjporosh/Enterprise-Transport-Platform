using System.Security.Claims;
using BusService.Application.Common.Interfaces;

namespace BusService.Api.Security;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Principal?.FindFirstValue("sub");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public Guid? TenantId
    {
        get
        {
            var value = Principal?.FindFirstValue("tenant_id") ?? Principal?.FindFirstValue("tenant");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public Guid? CompanyId
    {
        get
        {
            var value = Principal?.FindFirstValue("company_id") ?? Principal?.FindFirstValue("company");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public Guid? OrganizationId
    {
        get
        {
            var value = Principal?.FindFirstValue("organization_id") ?? Principal?.FindFirstValue("organization");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;
}
