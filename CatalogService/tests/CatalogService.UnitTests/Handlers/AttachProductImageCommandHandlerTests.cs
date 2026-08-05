using CatalogService.Application.Commands;
using CatalogService.Application.Common;
using CatalogService.Application.Handlers;
using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using NSubstitute;

namespace CatalogService.UnitTests.Handlers;

public class AttachProductImageCommandHandlerTests
{
    [Fact]
    public async Task Handle_ExistingProduct_CreatesImageRecordAndInvalidatesCache()
    {
        var productRepository = Substitute.For<IProductRepository>();
        var productImageRepository = Substitute.For<IProductImageRepository>();
        var blobStorageService = Substitute.For<IBlobStorageService>();
        var cacheService = Substitute.For<ICacheService>();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Widget",
            Sku = "SKU-001",
            Price = 10m,
            CategoryId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        blobStorageService.GetPublicUrl("products/x/y.png").Returns("https://minio/public/products/x/y.png");

        var handler = new AttachProductImageCommandHandler(productRepository, productImageRepository, blobStorageService, cacheService);
        var command = new AttachProductImageCommand(product.Id, "products/x/y.png", 0, true);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("https://minio/public/products/x/y.png", result.Value!.Url);
        Assert.True(result.Value.IsPrimary);
        await productImageRepository.Received(1).AddAsync(
            Arg.Is<ProductImage>(i => i!.ProductId == product.Id && i.ObjectKey == "products/x/y.png"),
            Arg.Any<CancellationToken>());
        await productImageRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await cacheService.Received(1).RemoveAsync(CacheKeys.Product(product.Id), Arg.Any<CancellationToken>());
        await cacheService.Received(1).RemoveByPrefixAsync(CacheKeys.ProductListPrefix, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownProduct_ReturnsFailure()
    {
        var productRepository = Substitute.For<IProductRepository>();
        var productImageRepository = Substitute.For<IProductImageRepository>();
        var blobStorageService = Substitute.For<IBlobStorageService>();
        var cacheService = Substitute.For<ICacheService>();
        productRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Product?)null);

        var handler = new AttachProductImageCommandHandler(productRepository, productImageRepository, blobStorageService, cacheService);
        var result = await handler.Handle(new AttachProductImageCommand(Guid.NewGuid(), "key", 0, false), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("Product not found.", result.Errors);
    }
}
