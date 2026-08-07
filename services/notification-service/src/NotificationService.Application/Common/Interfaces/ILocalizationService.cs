namespace NotificationService.Application.Common.Interfaces;

/// <summary>Resolves user/system-facing message strings (validation messages, API messages) for a given key + locale, with fallback to English — see Infrastructure/Localization and docs/programmers-guide/localization.md, "How to add a new language".</summary>
public interface ILocalizationService
{
    string GetString(string key, string? locale = null, params object[] args);
}
