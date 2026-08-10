namespace BookingService.Application.Common.Interfaces;

/// <summary>
/// Cache-aside abstraction backed by Redis in Infrastructure. Kept generic
/// and serialization-agnostic here so Application never references
/// StackExchange.Redis directly.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default);

    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}
