using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Infrastructure.Persistence;

public class ProductImageRepository : IProductImageRepository
{
    private readonly CatalogDbContext _dbContext;

    public ProductImageRepository(CatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ProductImage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.ProductImages.SingleOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductImage>> ListByProductIdsAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken = default)
    {
        var ids = productIds as ICollection<Guid> ?? productIds.ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        return await _dbContext.ProductImages
            .Where(i => ids.Contains(i.ProductId))
            .OrderBy(i => i.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ProductImage image, CancellationToken cancellationToken = default)
    {
        await _dbContext.ProductImages.AddAsync(image, cancellationToken);
    }

    public void Remove(ProductImage image)
    {
        _dbContext.ProductImages.Remove(image);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
