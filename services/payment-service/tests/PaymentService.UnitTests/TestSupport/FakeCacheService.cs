using PaymentService.Application.Common.Interfaces;

namespace PaymentService.UnitTests.TestSupport;

public class FakeCacheService : ICacheService
{
    private readonly Dictionary<string, object> _cache = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (_cache.TryGetValue(key, out var value) && value is T typedValue)
            return Task.FromResult<T?>(typedValue);

        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default) where T : class
    {
        _cache[key] = value;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var keysToRemove = _cache.Keys.Where(k => k.StartsWith(prefix)).ToList();
        foreach (var key in keysToRemove)
            _cache.Remove(key);

        return Task.CompletedTask;
    }
}
