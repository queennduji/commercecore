using CatalogService.Application.Commands;
using CatalogService.Application.Handlers;
using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using NSubstitute;

namespace CatalogService.UnitTests.Handlers;

public class DeleteCategoryCommandHandlerTests
{
    [Fact]
    public async Task Handle_CategoryWithNoProducts_Deletes()
    {
        var categoryRepository = Substitute.For<ICategoryRepository>();
        var productRepository = Substitute.For<IProductRepository>();

        var category = new Category { Id = Guid.NewGuid(), Name = "Electronics", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        categoryRepository.GetByIdAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);
        productRepository.AnyInCategoryAsync(category.Id, Arg.Any<CancellationToken>()).Returns(false);

        var handler = new DeleteCategoryCommandHandler(categoryRepository, productRepository);
        var result = await handler.Handle(new DeleteCategoryCommand(category.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
        categoryRepository.Received(1).Remove(category);
    }

    [Fact]
    public async Task Handle_CategoryWithProducts_ReturnsFailure()
    {
        var categoryRepository = Substitute.For<ICategoryRepository>();
        var productRepository = Substitute.For<IProductRepository>();

        var category = new Category { Id = Guid.NewGuid(), Name = "Electronics", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        categoryRepository.GetByIdAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);
        productRepository.AnyInCategoryAsync(category.Id, Arg.Any<CancellationToken>()).Returns(true);

        var handler = new DeleteCategoryCommandHandler(categoryRepository, productRepository);
        var result = await handler.Handle(new DeleteCategoryCommand(category.Id), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("Cannot delete a category that still has products assigned to it.", result.Errors);
        categoryRepository.DidNotReceive().Remove(Arg.Any<Category>());
    }

    [Fact]
    public async Task Handle_UnknownCategory_ReturnsFailure()
    {
        var categoryRepository = Substitute.For<ICategoryRepository>();
        var productRepository = Substitute.For<IProductRepository>();
        categoryRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Category?)null);

        var handler = new DeleteCategoryCommandHandler(categoryRepository, productRepository);
        var result = await handler.Handle(new DeleteCategoryCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
    }
}
