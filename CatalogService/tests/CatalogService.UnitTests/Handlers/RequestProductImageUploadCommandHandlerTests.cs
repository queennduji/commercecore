using CatalogService.Application.Commands;
using CatalogService.Application.Handlers;
using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using NSubstitute;

namespace CatalogService.UnitTests.Handlers;

public class RequestProductImageUploadCommandHandlerTests
{
    [Fact]
    public async Task Handle_ExistingProduct_ReturnsPresignedUploadUrl()
    {
        var productRepository = Substitute.For<IProductRepository>();
        var blobStorageService = Substitute.For<IBlobStorageService>();

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

        var expectedUrl = new PresignedUploadUrl("https://minio/upload", "products/x/y-file.png", "https://minio/public/x/y-file.png", DateTime.UtcNow.AddMinutes(5));
        blobStorageService.CreatePresignedUploadUrlAsync(Arg.Any<string>(), "image/png", Arg.Any<CancellationToken>())
            .Returns(expectedUrl);

        var handler = new RequestProductImageUploadCommandHandler(productRepository, blobStorageService);
        var result = await handler.Handle(new RequestProductImageUploadCommand(product.Id, "file.png", "image/png"), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(expectedUrl.UploadUrl, result.Value!.UploadUrl);
        await blobStorageService.Received(1).CreatePresignedUploadUrlAsync(
            Arg.Is<string>(key => key!.StartsWith($"products/{product.Id}/") && key.EndsWith("file.png")),
            "image/png",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownProduct_ReturnsFailure()
    {
        var productRepository = Substitute.For<IProductRepository>();
        var blobStorageService = Substitute.For<IBlobStorageService>();
        productRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Product?)null);

        var handler = new RequestProductImageUploadCommandHandler(productRepository, blobStorageService);
        var result = await handler.Handle(new RequestProductImageUploadCommand(Guid.NewGuid(), "file.png", "image/png"), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("Product not found.", result.Errors);
    }
}
