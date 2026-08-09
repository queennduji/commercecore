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

    [Fact]
    public async Task Handle_DuplicateNameUnderSameParent_ReturnsFailure()
    {
        var categoryRepository = Substitute.For<ICategoryRepository>();
        var existing = new Category { Id = Guid.NewGuid(), Name = "Electronics", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        categoryRepository.GetByNameAndParentAsync("Electronics", null, Arg.Any<CancellationToken>()).Returns(existing);

        var handler = new CreateCategoryCommandHandler(categoryRepository);
        var result = await handler.Handle(new CreateCategoryCommand("Electronics", "Gadgets", null), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("A category with this name already exists under this parent.", result.Errors);
        await categoryRepository.DidNotReceive().AddAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SameNameUnderDifferentParent_Succeeds()
    {
        var categoryRepository = Substitute.For<ICategoryRepository>();
        var parentId = Guid.NewGuid();
        var parent = new Category { Id = parentId, Name = "Phones", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        categoryRepository.GetByIdAsync(parentId, Arg.Any<CancellationToken>()).Returns(parent);
        // "Electronics" already exists at the top level, but this request creates it under a
        // different parent — GetByNameAndParentAsync(name, parentId) correctly returns null since
        // no sibling under *this* parent shares the name, so the create should succeed.
        categoryRepository.GetByNameAndParentAsync("Electronics", parentId, Arg.Any<CancellationToken>()).Returns((Category?)null);

        var handler = new CreateCategoryCommandHandler(categoryRepository);
        var result = await handler.Handle(new CreateCategoryCommand("Electronics", null, parentId), CancellationToken.None);

        Assert.True(result.Succeeded);
    }
}
