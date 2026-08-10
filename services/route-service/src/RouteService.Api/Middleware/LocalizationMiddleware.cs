using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Primitives;
using RouteService.Application.Common.Interfaces;

namespace RouteService.Api.Middleware;

public sealed class LocalizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILocalizationService _localizationService;

    public LocalizationMiddleware(RequestDelegate next, ILocalizationService localizationService)
    {
        _next = next;
        _localizationService = localizationService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var locale = ResolveLocale(context.Request);
        context.Items["Locale"] = locale;
        await _next(context);
    }

    private static string ResolveLocale(HttpRequest request)
    {
        if (request.Query.TryGetValue("lang", out var lang) && !StringValues.IsNullOrEmpty(lang))
            return lang.ToString()!;

        if (request.Headers.TryGetValue("Accept-Language", out var acceptLanguage) && !StringValues.IsNullOrEmpty(acceptLanguage))
            return acceptLanguage.ToString()!.Split(',').First().Trim();

        return "en";
    }
}
