using CartService.Application.Interfaces;

namespace CartService.IntegrationTests.Fixtures;

/// <summary>
/// Stands in for a real CatalogService in integration tests – spinning up CatalogService's own
/// Docker image just to test CartService's Redis-backed logic would be disproportionate. Tests
/// seed <see cref="Products"/> directly instead.
/// </summary>
public class FakeCatalogServiceClient : ICatalogServiceClient
{
    public Dictionary<Guid, CatalogProductSnapshot> Products { get; } = [];

    public Task<CatalogProductSnapshot?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        Products.TryGetValue(productId, out var product);
        return Task.FromResult(product);
    }
}
