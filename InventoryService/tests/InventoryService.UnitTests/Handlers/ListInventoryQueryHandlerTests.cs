using InventoryService.Application.Handlers;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Queries;
using InventoryService.Domain.Entities;
using NSubstitute;

namespace InventoryService.UnitTests.Handlers;

public class ListInventoryQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsPagedResult()
    {
        var inventoryItemRepository = Substitute.For<IInventoryItemRepository>();
        var items = new List<InventoryItem>
        {
            new() { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), LocationId = Guid.NewGuid(), OnHand = 5 }
        };
        inventoryItemRepository.ListAsync(null, null, 1, 20, Arg.Any<CancellationToken>()).Returns((items, 1));

        var handler = new ListInventoryQueryHandler(inventoryItemRepository);
        var result = await handler.Handle(new ListInventoryQuery(null, null, 1, 20), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Single(result.Value!.Items);
        Assert.Equal(1, result.Value.TotalCount);
    }
}
