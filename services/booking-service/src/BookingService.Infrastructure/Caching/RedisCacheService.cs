using System.Text.Json;
using BookingService.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BookingService.Infrastructure.Caching;

/// <summary>
/// Cache-aside implementation over Redis. Deliberately fails open: if Redis
/// is unreachable, cache operations log and no-op rather than throwing —
/// a cache outage should degrade search latency, not take the API down.
/// </summary>
public sealed class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly RedisOptions _options;
    private readonly ILogger<RedisCacheService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public RedisCacheService(IConnectionMultiplexer redis, IOptions<RedisOptions> options, ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _options = options.Value;
        _logger = logger;
    }

    private string Prefixed(string key) => $"{_options.InstanceName}{key}";

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(Prefixed(key));
                if (value.IsNullOrEmpty)
                         return default;
                         
            return JsonSerializer.Deserialize<T>(value.ToString()!, JsonOptions);            
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis GET failed for key {Key}; falling back to source of truth.", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var json = JsonSerializer.Serialize(value, JsonOptions);
            await db.StringSetAsync(Prefixed(key), json, ttl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis SET failed for key {Key}; continuing without caching this result.", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPrefix = Prefixed(prefix);
            var endpoints = _redis.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                var db = _redis.GetDatabase();
                await foreach (var key in server.KeysAsync(pattern: $"{fullPrefix}*"))
                {
                    await db.KeyDeleteAsync(key);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis prefix eviction failed for prefix {Prefix}; stale entries will expire on TTL instead.", prefix);
        }
    }
}
