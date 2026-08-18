using BookingService.Application.Common.Interfaces;

namespace BookingService.UnitTests.TestSupport;

/// <summary>In-memory stand-in for Redis so handler tests don't need a real cache — same cache-aside contract, no I/O.</summary>
public sealed class FakeCacheService : ICacheService
{
    private readonly Dictionary<string, object> _store = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TryGetValue(key, out var value) && value is T typed ? typed : default);

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        _store[key] = value!;
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        foreach (var key in _store.Keys.Where(k => k.StartsWith(prefix)).ToList())
            _store.Remove(key);
        return Task.CompletedTask;
    }
}
