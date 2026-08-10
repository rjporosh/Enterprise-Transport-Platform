using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaymentService.Application.Common.Interfaces;
using StackExchange.Redis;

namespace PaymentService.Infrastructure.Caching;

public class RedisCacheService : ICacheService, IDisposable
{
    private readonly ILogger<RedisCacheService> _logger;
    private readonly Lazy<ConnectionMultiplexer> _connection;
    private readonly string _instanceName;
    private readonly int _defaultTtlSeconds;
    private bool _disposed;

    public RedisCacheService(IOptions<RedisOptions> options, ILogger<RedisCacheService> logger)
    {
        _logger = logger;
        var opts = options.Value;
        _instanceName = opts.InstanceName;
        _defaultTtlSeconds = opts.DefaultTtlSeconds;

        _connection = new Lazy<ConnectionMultiplexer>(() =>
        {
            try
            {
                return ConnectionMultiplexer.Connect(opts.ConnectionString);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis connection failed. Cache will be unavailable.");
                return null!;
            }
        });
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (_connection.IsValueCreated && _connection.Value == null!)
            return null;

        try
        {
            var db = _connection.Value.GetDatabase();
            var value = await db.StringGetAsync($"{_instanceName}:{key}");
            return value.IsNullOrEmpty ? null : System.Text.Json.JsonSerializer.Deserialize<T>(value.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache get failed for key {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default) where T : class
    {
        if (_connection.IsValueCreated && _connection.Value == null!)
            return;

        try
        {
            var db = _connection.Value.GetDatabase();
            var serialized = System.Text.Json.JsonSerializer.Serialize(value);
            var expiry = ttl ?? TimeSpan.FromSeconds(_defaultTtlSeconds);
            await db.StringSetAsync($"{_instanceName}:{key}", serialized, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache set failed for key {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_connection.IsValueCreated && _connection.Value == null!)
            return;

        try
        {
            var db = _connection.Value.GetDatabase();
            await db.KeyDeleteAsync($"{_instanceName}:{key}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache remove failed for key {Key}", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        if (_connection.IsValueCreated && _connection.Value == null!)
            return;

        try
        {
            var server = _connection.Value.GetServer(_connection.Value.GetEndPoints().First());
            var keys = server.Keys(pattern: $"{_instanceName}:{prefix}*");

            var db = _connection.Value.GetDatabase();
            foreach (var key in keys)
            {
                await db.KeyDeleteAsync(key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache prefix remove failed for prefix {Prefix}", prefix);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_connection.IsValueCreated && _connection.Value != null!)
                _connection.Value.Dispose();

            _disposed = true;
        }
    }
}
