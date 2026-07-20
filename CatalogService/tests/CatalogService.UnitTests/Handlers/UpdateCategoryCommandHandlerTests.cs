using CatalogService.Application.Commands;
using CatalogService.Application.Handlers;
using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using NSubstitute;

namespace CatalogService.UnitTests.Handlers;

public class UpdateCategoryCommandHandlerTests
{
    [Fact]
    public async Task Handle_SelfAsParent_ReturnsFailure()
    {
        var categoryRepository = Substitute.For<ICategoryRepository>();
        var category = new Category { Id = Guid.NewGuid(), Name = "Electronics", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        categoryRepository.GetByIdAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);

        var handler = new UpdateCategoryCommandHandler(categoryRepository);
        var command = new UpdateCategoryCommand(category.Id, "Electronics", null, category.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("A category cannot be its own parent.", result.Errors);
    }

    [Fact]
    public async Task Handle_UnknownCategory_ReturnsFailure()
    {
        var categoryRepository = Substitute.For<ICategoryRepository>();
        categoryRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Category?)null);

        var handler = new UpdateCategoryCommandHandler(categoryRepository);
        var result = await handler.Handle(new UpdateCategoryCommand(Guid.NewGuid(), "Name", null, null), CancellationToken.None);

        Assert.False(result.Succeeded);
    }
}
