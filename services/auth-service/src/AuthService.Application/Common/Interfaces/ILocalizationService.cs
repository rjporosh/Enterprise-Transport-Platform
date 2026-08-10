namespace AuthService.Application.Common.Interfaces;

public interface ILocalizationService
{
    string Get(string key, string language = "en");
    string Get(string key, params object[] args);
}
