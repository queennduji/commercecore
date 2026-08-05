using CatalogService.Application.Common;
using CatalogService.Application.Dtos;
using CatalogService.Application.Handlers;
using CatalogService.Application.Interfaces;
using CatalogService.Application.Queries;
using CatalogService.Domain.Entities;
using NSubstitute;

namespace CatalogService.UnitTests.Handlers;

public class GetProductQueryHandlerTests
{
    [Fact]
    public async Task Handle_CacheHit_ReturnsCachedDtoWithoutQueryingRepository()
    {
        var productRepository = Substitute.For<IProductRepository>();
        var productImageRepository = Substitute.For<IProductImageRepository>();
        var cacheService = Substitute.For<ICacheService>();

        var productId = Guid.NewGuid();
        var cachedDto = new ProductDto(
            productId, "Widget", "Cached", "SKU-001", 10m, ProductStatus.Active.ToString(),
            Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, []);
        cacheService.GetAsync<ProductDto>(CacheKeys.Product(productId), Arg.Any<CancellationToken>())
            .Returns(cachedDto);

        var handler = new GetProductQueryHandler(productRepository, productImageRepository, cacheService);
        var result = await handler.Handle(new GetProductQuery(productId), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Same(cachedDto, result.Value);
        await productRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await cacheService.DidNotReceive().SetAsync(
            Arg.Any<string>(), Arg.Any<ProductDto>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CacheMiss_FetchesFromRepositoryAndPopulatesCache()
    {
        var productRepository = Substitute.For<IProductRepository>();
        var productImageRepository = Substitute.For<IProductImageRepository>();
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
        cacheService.GetAsync<ProductDto>(CacheKeys.Product(product.Id), Arg.Any<CancellationToken>())
            .Returns((ProductDto?)null);
        productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        productImageRepository.ListByProductIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProductImage>());

        var handler = new GetProductQueryHandler(productRepository, productImageRepository, cacheService);
        var result = await handler.Handle(new GetProductQuery(product.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(product.Id, result.Value!.Id);
        await productRepository.Received(1).GetByIdAsync(product.Id, Arg.Any<CancellationToken>());
        await cacheService.Received(1).SetAsync(
            CacheKeys.Product(product.Id),
            Arg.Is<ProductDto>(d => d.Id == product.Id),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownProduct_ReturnsFailure()
    {
        var productRepository = Substitute.For<IProductRepository>();
        var productImageRepository = Substitute.For<IProductImageRepository>();
        var cacheService = Substitute.For<ICacheService>();

        cacheService.GetAsync<ProductDto>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((ProductDto?)null);
        productRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Product?)null);

        var handler = new GetProductQueryHandler(productRepository, productImageRepository, cacheService);
        var result = await handler.Handle(new GetProductQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
    }
}
