using InventoryService.Application.Handlers;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Queries;
using InventoryService.Domain.Entities;
using NSubstitute;

namespace InventoryService.UnitTests.Handlers;

public class ListInventoryByProductQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsItemsAcrossLocations()
    {
        var inventoryItemRepository = Substitute.For<IInventoryItemRepository>();
        var productId = Guid.NewGuid();
        inventoryItemRepository.ListByProductIdAsync(productId, Arg.Any<CancellationToken>()).Returns(new List<InventoryItem>
        {
            new() { Id = Guid.NewGuid(), ProductId = productId, LocationId = Guid.NewGuid(), OnHand = 5 },
            new() { Id = Guid.NewGuid(), ProductId = productId, LocationId = Guid.NewGuid(), OnHand = 8 }
        });

        var handler = new ListInventoryByProductQueryHandler(inventoryItemRepository);
        var result = await handler.Handle(new ListInventoryByProductQuery(productId), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Value!.Count);
    }
}
