namespace BusService.Application.Common.Interfaces;

/// <summary>Cache-aside abstraction backed by Redis in Infrastructure — same contract as Auth/Booking Service.</summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}
