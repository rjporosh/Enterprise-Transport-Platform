using Microsoft.Extensions.Options;
using StackExchange.Redis;
using RouteService.Application.Common.Interfaces;

namespace RouteService.Infrastructure.Caching;

public sealed class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _connection;
    private readonly RedisOptions _options;
    private readonly IDatabase _database;

    public RedisCacheService(IConnectionMultiplexer connection, IOptions<RedisOptions> options)
    {
        _connection = connection;
        _options = options.Value;
        _database = _connection.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var value = await _database.StringGetAsync(key);
        if (!value.HasValue) return default;
        return System.Text.Json.JsonSerializer.Deserialize<T>(value.ToString());
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        await _database.StringSetAsync(key, json, ttl ?? _options.DefaultTtl);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _database.KeyDeleteAsync(key);
    }
}
