namespace BusService.Api.Security;

public static class ClientInfoExtensions
{
    public static string? GetClientIpAddress(this HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) && !string.IsNullOrWhiteSpace(forwardedFor))
            return forwardedFor.ToString().Split(',')[0].Trim();

        return context.Connection.RemoteIpAddress?.ToString();
    }

    public static string? GetUserAgent(this HttpContext context) =>
        context.Request.Headers.TryGetValue("User-Agent", out var userAgent) ? userAgent.ToString() : null;
}
