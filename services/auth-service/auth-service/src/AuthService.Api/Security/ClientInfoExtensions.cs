namespace AuthService.Api.Security;

/// <summary>
/// Resolves the caller's IP for audit/refresh-token-binding purposes.
/// Trusts X-Forwarded-For only because this service is designed to sit
/// behind the platform's API gateway/ingress, which is expected to strip
/// any client-supplied X-Forwarded-For and set its own — see
/// docs/architecture/auth-service-architecture.md, "Audit trail" for the
/// deployment assumption this relies on.
/// </summary>
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
