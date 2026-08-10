using Microsoft.Extensions.Primitives;
using PaymentService.Application.Common.Interfaces;

namespace PaymentService.Api.Middleware;

public class RequestContextMiddleware
{
    private readonly RequestDelegate _next;

    public RequestContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out StringValues tenantValues))
        {
            context.Items["TenantId"] = tenantValues.FirstOrDefault();
        }

        if (context.Request.Headers.TryGetValue("X-Company-Id", out StringValues companyValues))
        {
            context.Items["CompanyId"] = companyValues.FirstOrDefault();
        }

        if (context.Request.Headers.TryGetValue("X-Organization-Id", out StringValues orgValues))
        {
            context.Items["OrganizationId"] = orgValues.FirstOrDefault();
        }

        await _next(context);
    }
}
