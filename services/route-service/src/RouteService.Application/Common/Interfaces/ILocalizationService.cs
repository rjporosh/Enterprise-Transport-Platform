namespace RouteService.Application.Common.Interfaces;

public interface ILocalizationService
{
    string GetString(string key, string? locale = null, params object[] args);
}
