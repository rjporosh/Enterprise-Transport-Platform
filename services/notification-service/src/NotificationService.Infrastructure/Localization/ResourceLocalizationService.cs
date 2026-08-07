using System.Globalization;
using System.Resources;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Common.Interfaces;

namespace NotificationService.Infrastructure.Localization;

/// <summary>
/// Resource-based localization (CLAUDE.md, "Localization"): English is the
/// default/fallback culture; Bangla ("bn") is the second supported locale.
/// Adding a third language is config-free — drop a new
/// Messages.&lt;culture&gt;.resx next to Messages.resx (see
/// docs/programmers-guide/localization.md, "How to add a new language") —
/// no code change is required because ResourceManager resolves culture
/// fallback automatically.
/// </summary>
public sealed class ResourceLocalizationService : ILocalizationService
{
    private readonly ResourceManager _resourceManager;
    private readonly ILogger<ResourceLocalizationService> _logger;

    public ResourceLocalizationService(ILogger<ResourceLocalizationService> logger)
    {
        _logger = logger;
        _resourceManager = new ResourceManager(
            "NotificationService.Infrastructure.Localization.Resources.Messages",
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
        if (string.IsNullOrWhiteSpace(locale)) return CultureInfo.InvariantCulture; // resolves to Messages.resx (English default)

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
