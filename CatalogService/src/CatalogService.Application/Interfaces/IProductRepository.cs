using CatalogService.Domain.Entities;

namespace CatalogService.Application.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Product> Items, int TotalCount)> ListAsync(
        Guid? categoryId,
        ProductStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> AnyInCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);

    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
