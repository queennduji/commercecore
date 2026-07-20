using CatalogService.Domain.Entities;
using CatalogService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.UnitTests.Persistence;

public class ProductRepositoryTests
{
    private static CatalogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CatalogDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsTheSameProduct()
    {
        await using var dbContext = CreateDbContext();
        var repository = new ProductRepository(dbContext);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Widget",
            Sku = "SKU-001",
            Price = 9.99m,
            CategoryId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(product);
        await repository.SaveChangesAsync();

        var found = await repository.GetByIdAsync(product.Id);

        Assert.NotNull(found);
        Assert.Equal("Widget", found!.Name);
    }

    [Fact]
    public async Task ListAsync_FiltersByCategoryAndStatus_WithPaging()
    {
        await using var dbContext = CreateDbContext();
        var repository = new ProductRepository(dbContext);
        var categoryA = Guid.NewGuid();
        var categoryB = Guid.NewGuid();

        for (var i = 0; i < 5; i++)
        {
            await repository.AddAsync(new Product
            {
                Id = Guid.NewGuid(),
                Name = $"Product {i}",
                Sku = $"SKU-{i}",
                Price = 10m,
                Status = i % 2 == 0 ? ProductStatus.Active : ProductStatus.Draft,
                CategoryId = i < 3 ? categoryA : categoryB,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        await repository.SaveChangesAsync();

        var (items, totalCount) = await repository.ListAsync(categoryA, ProductStatus.Active, page: 1, pageSize: 10);

        Assert.Equal(2, totalCount);
        Assert.All(items, p => Assert.Equal(categoryA, p.CategoryId));
        Assert.All(items, p => Assert.Equal(ProductStatus.Active, p.Status));
    }

    [Fact]
    public async Task AnyInCategoryAsync_ReturnsTrueWhenProductsExist()
    {
        await using var dbContext = CreateDbContext();
        var repository = new ProductRepository(dbContext);
        var categoryId = Guid.NewGuid();

        await repository.AddAsync(new Product
        {
            Id = Guid.NewGuid(),
            Name = "Widget",
            Sku = "SKU-001",
            Price = 10m,
            CategoryId = categoryId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await repository.SaveChangesAsync();

        Assert.True(await repository.AnyInCategoryAsync(categoryId));
        Assert.False(await repository.AnyInCategoryAsync(Guid.NewGuid()));
    }
}
