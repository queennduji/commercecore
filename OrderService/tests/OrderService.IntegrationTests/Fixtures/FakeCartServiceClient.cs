using OrderService.Application.Interfaces;

namespace OrderService.IntegrationTests.Fixtures;

/// <summary>Stands in for a real CartService — spinning up its own Docker image just to test
/// OrderService's checkout saga would be disproportionate. Tests seed <see cref="Carts"/> directly.</summary>
public class FakeCartServiceClient : ICartServiceClient
{
    public Dictionary<Guid, CartSnapshot> Carts { get; } = [];
    public HashSet<Guid> ClearedUserIds { get; } = [];

    public Task<CartSnapshot?> GetCartAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        Carts.TryGetValue(userId, out var cart);
        return Task.FromResult(cart);
    }

    public Task ClearCartAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        ClearedUserIds.Add(userId);
        Carts.Remove(userId);
        return Task.CompletedTask;
    }
}
