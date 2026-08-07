using InventoryService.Application.Handlers;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Queries;
using InventoryService.Domain.Entities;
using NSubstitute;

namespace InventoryService.UnitTests.Handlers;

public class GetInventoryItemQueryHandlerTests
{
    [Fact]
    public async Task Handle_ExistingItem_ReturnsDto()
    {
        var inventoryItemRepository = Substitute.For<IInventoryItemRepository>();
        var productId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var item = new InventoryItem { Id = Guid.NewGuid(), ProductId = productId, LocationId = locationId, OnHand = 15, Reserved = 3 };
        inventoryItemRepository.GetByProductAndLocationAsync(productId, locationId, Arg.Any<CancellationToken>()).Returns(item);

        var handler = new GetInventoryItemQueryHandler(inventoryItemRepository);
        var result = await handler.Handle(new GetInventoryItemQuery(productId, locationId), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(12, result.Value!.Available);
    }

    [Fact]
    public async Task Handle_UnknownItem_ReturnsFailure()
    {
        var inventoryItemRepository = Substitute.For<IInventoryItemRepository>();
        inventoryItemRepository.GetByProductAndLocationAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((InventoryItem?)null);

        var handler = new GetInventoryItemQueryHandler(inventoryItemRepository);
        var result = await handler.Handle(new GetInventoryItemQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
    }
}
