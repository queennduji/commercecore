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

    [Fact]
    public async Task Handle_RenamingToAnotherSiblingsName_ReturnsFailure()
    {
        var categoryRepository = Substitute.For<ICategoryRepository>();
        var category = new Category { Id = Guid.NewGuid(), Name = "Phones", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var otherSibling = new Category { Id = Guid.NewGuid(), Name = "Electronics", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        categoryRepository.GetByIdAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);
        categoryRepository.GetByNameAndParentAsync("Electronics", null, Arg.Any<CancellationToken>()).Returns(otherSibling);

        var handler = new UpdateCategoryCommandHandler(categoryRepository);
        var result = await handler.Handle(new UpdateCategoryCommand(category.Id, "Electronics", null, null), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("A category with this name already exists under this parent.", result.Errors);
    }

    [Fact]
    public async Task Handle_KeepingOwnCurrentName_Succeeds()
    {
        var categoryRepository = Substitute.For<ICategoryRepository>();
        var category = new Category { Id = Guid.NewGuid(), Name = "Electronics", Description = "Old", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        categoryRepository.GetByIdAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);
        // GetByNameAndParentAsync legitimately finds the category itself here – the handler must
        // not treat that self-match as a conflict.
        categoryRepository.GetByNameAndParentAsync("Electronics", null, Arg.Any<CancellationToken>()).Returns(category);

        var handler = new UpdateCategoryCommandHandler(categoryRepository);
        var result = await handler.Handle(new UpdateCategoryCommand(category.Id, "Electronics", "New description", null), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("New description", result.Value!.Description);
    }
}
