namespace AuthService.Application.Common.Interfaces;

/// <summary>
/// Cache-aside abstraction backed by Redis in Infrastructure. Used here for
/// two things: caching the resolved role list for GetCurrentUser (hot path,
/// hit on every authenticated request through the gateway), and as a
/// distributed denylist for access tokens revoked before their natural
/// expiry (logout-everywhere) — see docs/architecture/auth-service-architecture.md.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default);

    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}
