using System.Globalization;
using System.Resources;
using Microsoft.Extensions.Logging;
using RouteService.Application.Common.Interfaces;

namespace RouteService.Infrastructure.Localization;

public sealed class ResourceLocalizationService : ILocalizationService
{
    private readonly ResourceManager _resourceManager;
    private readonly ILogger<ResourceLocalizationService> _logger;

    public ResourceLocalizationService(ILogger<ResourceLocalizationService> logger)
    {
        _logger = logger;
        _resourceManager = new ResourceManager(
            "RouteService.Infrastructure.Localization.Resources.Messages",
            typeof(ResourceLocalizationService).Assembly);
    }

    public string GetString(string key, string? locale = null, params object[] args)
    {
        var culture = ResolveCulture(locale);

        string? value;
        try
        {
            value = _resourceManager.GetString(key, culture);
        }
        catch (MissingManifestResourceException ex)
        {
            _logger.LogError(ex, "Localization resource manifest is missing for key '{Key}'.", key);
            value = null;
        }

        if (value is null)
        {
            _logger.LogWarning("Missing localization key '{Key}' for locale '{Locale}'; falling back to the key itself.", key, culture.Name);
            return key;
        }

        return args.Length == 0 ? value : string.Format(culture, value, args);
    }

    private static CultureInfo ResolveCulture(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale)) return CultureInfo.InvariantCulture;
        try
        {
            return CultureInfo.GetCultureInfo(locale);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.InvariantCulture;
        }
    }
}
