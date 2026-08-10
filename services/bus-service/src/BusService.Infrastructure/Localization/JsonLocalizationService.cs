using System.Globalization;
using BusService.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace BusService.Infrastructure.Localization;

public sealed class JsonLocalizationService : ILocalizationService
{
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private const string CacheKeyPrefix = "localization:";

    public JsonLocalizationService(IMemoryCache cache, IConfiguration configuration)
    {
        _cache = cache;
        _configuration = configuration;
    }

    public string GetString(string key)
    {
        var culture = GetCurrentCulture();
        var translations = LoadTranslations(culture);
        return translations.TryGetValue(key, out var value) ? value : key;
    }

    public string GetString(string key, params object[] args)
    {
        var template = GetString(key);
        return string.Format(CultureInfo.CurrentCulture, template, args);
    }

    private string GetCurrentCulture()
    {
        var culture = _configuration["Localization:DefaultCulture"] ?? "en";
        return culture.StartsWith("bn", StringComparison.OrdinalIgnoreCase) ? "bn" : "en";
    }

    private IReadOnlyDictionary<string, string> LoadTranslations(string culture)
    {
        var cacheKey = $"{CacheKeyPrefix}{culture}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyDictionary<string, string>? cached))
            return cached!;

        var translations = culture == "bn"
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["BUS_REGISTERED_SUCCESS"] = "বাস সফলভাবে নিবন্ধিত হয়েছে।",
                ["BUS_UPDATED_SUCCESS"] = "বাসের বিবরণ আপডেট করা হয়েছে।",
                ["BUS_STATUS_CHANGED"] = "বাসের স্ট্যাটাস {0} এ পরিবর্তন করা হয়েছে।",
                ["DEPOT_CREATED_SUCCESS"] = "ডিপট সফলভাবে তৈরি হয়েছে।",
                ["BUS_NOT_FOUND"] = "বাস পাওয়া যায়নি।",
                ["DEPOT_NOT_FOUND"] = "ডিপট পাওয়া যায়নি।",
                ["DUPLICATE_PLATE_NUMBER"] = "এই প্লেট নম্বর দিয়ে একটি বাস ইতিমধ্যে রয়েছে।",
                ["INVALID_STATUS_TRANSITION"] = "অবৈধ স্ট্যাটাস ট্রানজিশন।",
                ["RATE_LIMIT_EXCEEDED"] = "অনুরোধের সীমা অতিক্রম করা হয়েছে। অনুগ্রহ করে কিছুক্ষণ পর আবার চেষ্টা করুন।"
            }
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["BUS_REGISTERED_SUCCESS"] = "Bus registered successfully.",
                ["BUS_UPDATED_SUCCESS"] = "Bus details updated successfully.",
                ["BUS_STATUS_CHANGED"] = "Bus status changed to {0}.",
                ["DEPOT_CREATED_SUCCESS"] = "Depot created successfully.",
                ["BUS_NOT_FOUND"] = "Bus not found.",
                ["DEPOT_NOT_FOUND"] = "Depot not found.",
                ["DUPLICATE_PLATE_NUMBER"] = "A bus with this plate number already exists.",
                ["INVALID_STATUS_TRANSITION"] = "Invalid status transition.",
                ["RATE_LIMIT_EXCEEDED"] = "Rate limit exceeded. Please retry after the window resets."
            };

        _cache.Set(cacheKey, translations, TimeSpan.FromHours(1));
        return translations;
    }
}
