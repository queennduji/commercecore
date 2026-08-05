using CatalogService.Application.Commands;
using CatalogService.Application.Common;
using CatalogService.Application.Handlers;
using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using CatalogService.Domain.Events;
using NSubstitute;

namespace CatalogService.UnitTests.Handlers;

public class CreateProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_CreatesProductAndPublishesEvent()
    {
        var productRepository = Substitute.For<IProductRepository>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var cacheService = Substitute.For<ICacheService>();
        var handler = new CreateProductCommandHandler(productRepository, eventPublisher, cacheService);
        var categoryId = Guid.NewGuid();

        var command = new CreateProductCommand("Widget", "A widget", "SKU-001", 19.99m, categoryId);
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Widget", result.Value!.Name);
        Assert.Equal("SKU-001", result.Value.Sku);
        Assert.Equal(ProductStatus.Draft.ToString(), result.Value.Status);
        Assert.Equal(categoryId, result.Value.CategoryId);

        await productRepository.Received(1).AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
        await productRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await eventPublisher.Received(1).PublishProductCreatedAsync(
            Arg.Is<ProductCreatedEvent>(e => e!.Sku == "SKU-001"),
            Arg.Any<CancellationToken>());
        await cacheService.Received(1).RemoveByPrefixAsync(CacheKeys.ProductListPrefix, Arg.Any<CancellationToken>());
    }
}
