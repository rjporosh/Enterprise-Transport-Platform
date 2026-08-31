using Microsoft.AspNetCore.Http;

namespace Platform.Common.Security;

/// <summary>
/// Adds conservative security response headers appropriate for a JSON API edge.
/// Applied at the gateway so every downstream response inherits them without
/// each service repeating the config.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(static state =>
        {
            var ctx = (HttpContext)state;
            var h = ctx.Response.Headers;

            h["X-Content-Type-Options"] = "nosniff";
            h["X-Frame-Options"] = "DENY";
            h["Referrer-Policy"] = "no-referrer";
            h["Cross-Origin-Resource-Policy"] = "same-site";
            h["X-Permitted-Cross-Domain-Policies"] = "none";

            // API responses are never a document context; lock scripting down hard.
            h["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

            // HSTS only makes sense over TLS; the terminating proxy / ingress
            // owns it in production. Left out here to avoid poisoning plain-HTTP
            // local dev.

            // Never advertise the server implementation.
            h.Remove("Server");
            h.Remove("X-Powered-By");

            return Task.CompletedTask;
        }, context);

        await next(context);
    }
}
