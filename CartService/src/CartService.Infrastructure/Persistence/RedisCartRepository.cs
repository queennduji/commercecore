using System.Text.Json;
using CartService.Application.Interfaces;
using CartService.Domain.Entities;
using CartService.Infrastructure.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CartService.Infrastructure.Persistence;

/// <summary>
/// Redis is the primary store for carts (not a cache sitting in front of a database): each cart is
/// one JSON-serialized key, with its TTL refreshed on every write so an abandoned cart expires
/// naturally instead of needing a cleanup job.
/// </summary>
public class RedisCartRepository : ICartRepository
{
    private const string KeyPrefix = "cart:";

    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly RedisOptions _options;

    public RedisCartRepository(IConnectionMultiplexer connectionMultiplexer, IOptions<RedisOptions> options)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _options = options.Value;
    }

    public async Task<Cart?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var db = _connectionMultiplexer.GetDatabase();
        var value = await db.StringGetAsync(Key(id));
        return value.HasValue ? JsonSerializer.Deserialize<Cart>((string)value!) : null;
    }

    public async Task SaveAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        var db = _connectionMultiplexer.GetDatabase();
        var json = JsonSerializer.Serialize(cart);
        await db.StringSetAsync(Key(cart.Id), json, TimeSpan.FromDays(_options.TtlDays));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var db = _connectionMultiplexer.GetDatabase();
        await db.KeyDeleteAsync(Key(id));
    }

    private static string Key(Guid id) => $"{KeyPrefix}{id}";
}
