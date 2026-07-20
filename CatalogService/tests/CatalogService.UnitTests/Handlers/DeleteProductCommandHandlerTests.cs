using CatalogService.Application.Commands;
using CatalogService.Application.Handlers;
using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using CatalogService.Domain.Events;
using NSubstitute;

namespace CatalogService.UnitTests.Handlers;

public class DeleteProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_ExistingProduct_ArchivesInsteadOfDeleting()
    {
        var productRepository = Substitute.For<IProductRepository>();
        var eventPublisher = Substitute.For<IEventPublisher>();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Widget",
            Sku = "SKU-001",
            Price = 10m,
            Status = ProductStatus.Active,
            CategoryId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var handler = new DeleteProductCommandHandler(productRepository, eventPublisher);
        var result = await handler.Handle(new DeleteProductCommand(product.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(ProductStatus.Archived, product.Status);
        await eventPublisher.Received(1).PublishProductDeletedAsync(Arg.Any<ProductDeletedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownProduct_ReturnsFailure()
    {
        var productRepository = Substitute.For<IProductRepository>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        productRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Product?)null);

        var handler = new DeleteProductCommandHandler(productRepository, eventPublisher);
        var result = await handler.Handle(new DeleteProductCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
    }
}
