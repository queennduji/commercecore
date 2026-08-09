using CatalogService.Domain.Entities;

namespace CatalogService.Application.Interfaces;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Category?> GetByNameAndParentAsync(string name, Guid? parentCategoryId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Category category, CancellationToken cancellationToken = default);

    void Remove(Category category);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
