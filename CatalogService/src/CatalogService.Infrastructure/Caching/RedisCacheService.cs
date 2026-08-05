using System.Text.Json;
using CatalogService.Application.Interfaces;
using CatalogService.Infrastructure.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CatalogService.Infrastructure.Caching;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly RedisOptions _options;

    public RedisCacheService(IConnectionMultiplexer connectionMultiplexer, IOptions<RedisOptions> options)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _options = options.Value;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var db = _connectionMultiplexer.GetDatabase();
        var value = await db.StringGetAsync(key);
        return value.HasValue ? JsonSerializer.Deserialize<T>((string)value!) : null;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default) where T : class
    {
        var db = _connectionMultiplexer.GetDatabase();
        var json = JsonSerializer.Serialize(value);
        await db.StringSetAsync(key, json, ttl ?? TimeSpan.FromSeconds(_options.DefaultTtlSeconds));
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var db = _connectionMultiplexer.GetDatabase();
        await db.KeyDeleteAsync(key);
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var db = _connectionMultiplexer.GetDatabase();
        var endpoint = _connectionMultiplexer.GetEndPoints()[0];
        var server = _connectionMultiplexer.GetServer(endpoint);

        await foreach (var key in server.KeysAsync(pattern: $"{prefix}*"))
        {
            await db.KeyDeleteAsync(key);
        }
    }
}
