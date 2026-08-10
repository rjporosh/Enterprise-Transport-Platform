using System.Globalization;

namespace NotificationService.Api.Middleware;

/// <summary>
/// Resolves the request's locale (CLAUDE.md, "Localization" — Accept-Language
/// header, then ?lang= query parameter, then falls back to English) and sets
/// it as the current thread culture so ILocalizationService.GetString calls
/// made without an explicit locale argument anywhere downstream in the
/// request still resolve correctly. "User Preference" (a signed-in user's
/// saved locale) is intentionally not read here — this service has no
/// concept of the caller's identity/profile beyond a bearer token'\''s claims,
/// and RecipientPreference.Locale is used explicitly by SendNotificationHandler
/// for the notification's *own* content locale, which is a separate concern
/// from the locale of *this* API response.
/// </summary>
public sealed class LocalizationMiddleware
{
    private static readonly string[] SupportedCultures = { "en", "bn" };
    private readonly RequestDelegate _next;

    public LocalizationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var locale = ResolveFromQuery(context) ?? ResolveFromHeader(context) ?? "en";
        var culture = CultureInfo.GetCultureInfo(locale);

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        await _next(context);
    }

    private static string? ResolveFromQuery(HttpContext context) =>
        context.Request.Query.TryGetValue("lang", out var lang) && SupportedCultures.Contains(lang.ToString())
            ? lang.ToString()
            : null;

    private static string? ResolveFromHeader(HttpContext context)
    {
        var header = context.Request.Headers.AcceptLanguage.ToString();
        if (string.IsNullOrWhiteSpace(header)) return null;

        foreach (var segment in header.Split(','))
        {
            var candidate = segment.Split(';')[0].Trim();
            var primary = candidate.Split('-')[0].ToLowerInvariant();
            if (SupportedCultures.Contains(primary)) return primary;
        }

        return null;
    }
}
