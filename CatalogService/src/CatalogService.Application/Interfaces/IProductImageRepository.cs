using CatalogService.Domain.Entities;

namespace CatalogService.Application.Interfaces;

public interface IProductImageRepository
{
    Task<ProductImage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductImage>> ListByProductIdsAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken = default);

    Task AddAsync(ProductImage image, CancellationToken cancellationToken = default);

    void Remove(ProductImage image);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
