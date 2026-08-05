using CatalogService.Application.Commands;
using CatalogService.Application.Common;
using CatalogService.Application.Handlers;
using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using NSubstitute;

namespace CatalogService.UnitTests.Handlers;

public class DeleteProductImageCommandHandlerTests
{
    [Fact]
    public async Task Handle_ExistingImage_RemovesRowAndDeletesBlobAndInvalidatesCache()
    {
        var productImageRepository = Substitute.For<IProductImageRepository>();
        var blobStorageService = Substitute.For<IBlobStorageService>();
        var cacheService = Substitute.For<ICacheService>();

        var productId = Guid.NewGuid();
        var image = new ProductImage
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ObjectKey = "products/x/y.png",
            Url = "https://minio/public/products/x/y.png",
            CreatedAt = DateTime.UtcNow
        };
        productImageRepository.GetByIdAsync(image.Id, Arg.Any<CancellationToken>()).Returns(image);

        var handler = new DeleteProductImageCommandHandler(productImageRepository, blobStorageService, cacheService);
        var result = await handler.Handle(new DeleteProductImageCommand(productId, image.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
        productImageRepository.Received(1).Remove(image);
        await blobStorageService.Received(1).DeleteAsync("products/x/y.png", Arg.Any<CancellationToken>());
        await cacheService.Received(1).RemoveAsync(CacheKeys.Product(productId), Arg.Any<CancellationToken>());
        await cacheService.Received(1).RemoveByPrefixAsync(CacheKeys.ProductListPrefix, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ImageBelongsToDifferentProduct_ReturnsFailure()
    {
        var productImageRepository = Substitute.For<IProductImageRepository>();
        var blobStorageService = Substitute.For<IBlobStorageService>();
        var cacheService = Substitute.For<ICacheService>();

        var image = new ProductImage
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            ObjectKey = "products/x/y.png",
            Url = "https://minio/public/products/x/y.png",
            CreatedAt = DateTime.UtcNow
        };
        productImageRepository.GetByIdAsync(image.Id, Arg.Any<CancellationToken>()).Returns(image);

        var handler = new DeleteProductImageCommandHandler(productImageRepository, blobStorageService, cacheService);
        var result = await handler.Handle(new DeleteProductImageCommand(Guid.NewGuid(), image.Id), CancellationToken.None);

        Assert.False(result.Succeeded);
        await blobStorageService.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await cacheService.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownImage_ReturnsFailure()
    {
        var productImageRepository = Substitute.For<IProductImageRepository>();
        var blobStorageService = Substitute.For<IBlobStorageService>();
        var cacheService = Substitute.For<ICacheService>();
        productImageRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ProductImage?)null);

        var handler = new DeleteProductImageCommandHandler(productImageRepository, blobStorageService, cacheService);
        var result = await handler.Handle(new DeleteProductImageCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
    }
}
