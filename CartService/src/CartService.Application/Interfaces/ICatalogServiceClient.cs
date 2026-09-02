namespace CartService.Application.Interfaces;

public record CatalogProductSnapshot(Guid ProductId, string Sku, string Name, decimal Price, string Status);

/// <summary>
/// Synchronous HTTP call to CatalogService's public GET /api/products/{id} – the one place this
/// service reaches across a service boundary directly instead of via Kafka. Used at add-to-cart
/// time to validate the product exists/is active and to snapshot its current name/sku/price.
/// </summary>
public interface ICatalogServiceClient
{
    Task<CatalogProductSnapshot?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default);
}
