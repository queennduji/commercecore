using CatalogService.Application.Common;
using CatalogService.Application.Dtos;
using CatalogService.Application.Handlers;
using CatalogService.Application.Interfaces;
using CatalogService.Application.Queries;
using CatalogService.Domain.Entities;
using NSubstitute;

namespace CatalogService.UnitTests.Handlers;

public class ListProductsQueryHandlerTests
{
    [Fact]
    public async Task Handle_CacheHit_ReturnsCachedResultWithoutQueryingRepository()
    {
        var productRepository = Substitute.For<IProductRepository>();
        var productImageRepository = Substitute.For<IProductImageRepository>();
        var cacheService = Substitute.For<ICacheService>();

        var query = new ListProductsQuery(null, null, 1, 20);
        var cacheKey = CacheKeys.ProductList(query.CategoryId, query.Status, query.Page, query.PageSize);
        var cachedResult = new PagedResult<ProductDto>([], 1, 20, 0);
        cacheService.GetAsync<PagedResult<ProductDto>>(cacheKey, Arg.Any<CancellationToken>())
            .Returns(cachedResult);

        var handler = new ListProductsQueryHandler(productRepository, productImageRepository, cacheService);
        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Same(cachedResult, result.Value);
        await productRepository.DidNotReceive().ListAsync(
            Arg.Any<Guid?>(), Arg.Any<ProductStatus?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await cacheService.DidNotReceive().SetAsync(
            Arg.Any<string>(), Arg.Any<PagedResult<ProductDto>>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CacheMiss_FetchesFromRepositoryAndPopulatesCache()
    {
        var productRepository = Substitute.For<IProductRepository>();
        var productImageRepository = Substitute.For<IProductImageRepository>();
        var cacheService = Substitute.For<ICacheService>();

        var query = new ListProductsQuery(null, null, 1, 20);
        var cacheKey = CacheKeys.ProductList(query.CategoryId, query.Status, query.Page, query.PageSize);
        cacheService.GetAsync<PagedResult<ProductDto>>(cacheKey, Arg.Any<CancellationToken>())
            .Returns((PagedResult<ProductDto>?)null);

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
        productRepository.ListAsync(null, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<Product> { product }, 1));
        productImageRepository.ListByProductIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProductImage>());

        var handler = new ListProductsQueryHandler(productRepository, productImageRepository, cacheService);
        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Single(result.Value!.Items);
        await productRepository.Received(1).ListAsync(null, null, 1, 20, Arg.Any<CancellationToken>());
        await cacheService.Received(1).SetAsync(
            cacheKey,
            Arg.Is<PagedResult<ProductDto>>(r => r.Items.Count == 1 && r.TotalCount == 1),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }
}
