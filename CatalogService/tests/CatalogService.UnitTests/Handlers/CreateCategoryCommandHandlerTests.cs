using CatalogService.Application.Commands;
using CatalogService.Application.Handlers;
using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using NSubstitute;

namespace CatalogService.UnitTests.Handlers;

public class CreateCategoryCommandHandlerTests
{
    [Fact]
    public async Task Handle_NoParent_CreatesCategory()
    {
        var categoryRepository = Substitute.For<ICategoryRepository>();
        var handler = new CreateCategoryCommandHandler(categoryRepository);

        var result = await handler.Handle(new CreateCategoryCommand("Electronics", "Gadgets", null), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Electronics", result.Value!.Name);
        await categoryRepository.Received(1).AddAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownParent_ReturnsFailure()
    {
        var categoryRepository = Substitute.For<ICategoryRepository>();
        var parentId = Guid.NewGuid();
        categoryRepository.GetByIdAsync(parentId, Arg.Any<CancellationToken>()).Returns((Category?)null);

        var handler = new CreateCategoryCommandHandler(categoryRepository);
        var result = await handler.Handle(new CreateCategoryCommand("Phones", null, parentId), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("Parent category not found.", result.Errors);
    }
}
