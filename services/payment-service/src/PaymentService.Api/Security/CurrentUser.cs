using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace PaymentService.Api.Security;

public class CurrentUser : PaymentService.Application.Common.Interfaces.ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub");

            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                return userId;

            return null;
        }
    }

    public Guid? CustomerId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("customer_id");
            return claim != null && Guid.TryParse(claim.Value, out var id) ? id : UserId;
        }
    }

    public string? TenantId => _httpContextAccessor.HttpContext?.Items["TenantId"]?.ToString()
        ?? _httpContextAccessor.HttpContext?.User?.FindFirst("tenant_id")?.Value;

    public Guid? CompanyId
    {
        get
        {
            var companyIdStr = _httpContextAccessor.HttpContext?.Items["CompanyId"]?.ToString();
            if (companyIdStr != null && Guid.TryParse(companyIdStr, out var companyId))
                return companyId;

            return null;
        }
    }

    public Guid? OrganizationId
    {
        get
        {
            var orgIdStr = _httpContextAccessor.HttpContext?.Items["OrganizationId"]?.ToString();
            if (orgIdStr != null && Guid.TryParse(orgIdStr, out var orgId))
                return orgId;

            return null;
        }
    }

    public bool IsInRole(string role)
    {
        return _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
    }
}
