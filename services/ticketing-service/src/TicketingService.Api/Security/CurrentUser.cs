using System.Security.Claims;
using TicketingService.Application.Common.Interfaces;

namespace TicketingService.Api.Security;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? P => accessor.HttpContext?.User;

    public Guid? UserId => Parse(P?.FindFirstValue(ClaimTypes.NameIdentifier) ?? P?.FindFirstValue("sub"));
    public Guid? CustomerId => Parse(P?.FindFirstValue("customer_id")) ?? UserId;
    public bool IsInRole(string role) => P?.IsInRole(role) ?? false;

    private static Guid? Parse(string? v) => Guid.TryParse(v, out var g) ? g : null;
}
