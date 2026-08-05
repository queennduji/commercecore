using CatalogService.Application.Commands;
using CatalogService.Application.Common;
using CatalogService.Application.Handlers;
using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using NSubstitute;

namespace CatalogService.UnitTests.Handlers;

public class UpdateProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_ExistingProduct_UpdatesFieldsPublishesEventAndInvalidatesCache()
    {
        var productRepository = Substitute.For<IProductRepository>();
        var productImageRepository = Substitute.For<IProductImageRepository>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var cacheService = Substitute.For<ICacheService>();
        var categoryId = Guid.NewGuid();
        var newCategoryId = Guid.NewGuid();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Old Name",
            Sku = "SKU-001",
            Price = 10m,
            Status = ProductStatus.Draft,
            CategoryId = categoryId,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        productImageRepository.ListByProductIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProductImage>());

        var handler = new UpdateProductCommandHandler(productRepository, productImageRepository, eventPublisher, cacheService);
        var command = new UpdateProductCommand(product.Id, "New Name", "Updated", 25m, ProductStatus.Active, newCategoryId);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("New Name", result.Value!.Name);
        Assert.Equal(25m, result.Value.Price);
        Assert.Equal(ProductStatus.Active.ToString(), result.Value.Status);
        Assert.Equal(newCategoryId, result.Value.CategoryId);
        await eventPublisher.Received(1).PublishProductUpdatedAsync(Arg.Any<Domain.Events.ProductUpdatedEvent>(), Arg.Any<CancellationToken>());
        await cacheService.Received(1).RemoveAsync(CacheKeys.Product(product.Id), Arg.Any<CancellationToken>());
        await cacheService.Received(1).RemoveByPrefixAsync(CacheKeys.ProductListPrefix, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownProduct_ReturnsFailure()
    {
        var productRepository = Substitute.For<IProductRepository>();
        var productImageRepository = Substitute.For<IProductImageRepository>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var cacheService = Substitute.For<ICacheService>();
        productRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Product?)null);

        var handler = new UpdateProductCommandHandler(productRepository, productImageRepository, eventPublisher, cacheService);
        var command = new UpdateProductCommand(Guid.NewGuid(), "Name", null, 10m, ProductStatus.Draft, Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("Product not found.", result.Errors);
    }
}
