using System.Security.Claims;
using AuthService.Application.Common.Interfaces;

namespace AuthService.Api.Security;

/// <summary>Reads identity from the already-validated JWT on HttpContext.User — see Program.cs for the JwtBearer configuration that populates it.</summary>
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

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email) ?? Principal?.FindFirstValue("email");

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;
}
