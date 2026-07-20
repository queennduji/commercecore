using CatalogService.Domain.Entities;
using CatalogService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.UnitTests.Persistence;

public class CategoryRepositoryTests
{
    private static CatalogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CatalogDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ThenListAsync_ReturnsInsertedCategory()
    {
        await using var dbContext = CreateDbContext();
        var repository = new CategoryRepository(dbContext);

        var category = new Category { Id = Guid.NewGuid(), Name = "Electronics", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        await repository.AddAsync(category);
        await repository.SaveChangesAsync();

        var all = await repository.ListAsync();

        Assert.Single(all);
        Assert.Equal("Electronics", all[0].Name);
    }

    [Fact]
    public async Task Remove_ThenSaveChanges_DeletesCategory()
    {
        await using var dbContext = CreateDbContext();
        var repository = new CategoryRepository(dbContext);

        var category = new Category { Id = Guid.NewGuid(), Name = "Electronics", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        await repository.AddAsync(category);
        await repository.SaveChangesAsync();

        repository.Remove(category);
        await repository.SaveChangesAsync();

        var found = await repository.GetByIdAsync(category.Id);
        Assert.Null(found);
    }
}
