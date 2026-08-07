using InventoryService.Application.Commands;
using InventoryService.Application.Handlers;
using InventoryService.Application.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Events;
using NSubstitute;

namespace InventoryService.UnitTests.Handlers;

public class AdjustStockCommandHandlerTests
{
    [Fact]
    public async Task Handle_NewInventoryRecord_CreatesItAndAppliesPositiveDelta()
    {
        var inventoryItemRepository = Substitute.For<IInventoryItemRepository>();
        var locationRepository = Substitute.For<ILocationRepository>();
        var eventPublisher = Substitute.For<IEventPublisher>();

        var productId = Guid.NewGuid();
        var location = new Location { Id = Guid.NewGuid(), Name = "A", Code = "WH-A", IsActive = true };
        locationRepository.GetByIdAsync(location.Id, Arg.Any<CancellationToken>()).Returns(location);
        inventoryItemRepository.GetByProductAndLocationAsync(productId, location.Id, Arg.Any<CancellationToken>())
            .Returns((InventoryItem?)null);

        var handler = new AdjustStockCommandHandler(inventoryItemRepository, locationRepository, eventPublisher);
        var result = await handler.Handle(new AdjustStockCommand(productId, location.Id, 10, "initial stock"), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(10, result.Value!.OnHand);
        Assert.Equal(0, result.Value.Reserved);
        Assert.Equal(10, result.Value.Available);
        await inventoryItemRepository.Received(1).AddAsync(Arg.Any<InventoryItem>(), Arg.Any<CancellationToken>());
        await eventPublisher.Received(1).PublishStockAdjustedAsync(
            Arg.Is<StockAdjustedEvent>(e => e.Delta == 10 && e.OnHandAfter == 10), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingInventoryRecord_AppliesDelta()
    {
        var inventoryItemRepository = Substitute.For<IInventoryItemRepository>();
        var locationRepository = Substitute.For<ILocationRepository>();
        var eventPublisher = Substitute.For<IEventPublisher>();

        var productId = Guid.NewGuid();
        var location = new Location { Id = Guid.NewGuid(), Name = "A", Code = "WH-A", IsActive = true };
        var item = new InventoryItem { Id = Guid.NewGuid(), ProductId = productId, LocationId = location.Id, OnHand = 20, Reserved = 5 };
        locationRepository.GetByIdAsync(location.Id, Arg.Any<CancellationToken>()).Returns(location);
        inventoryItemRepository.GetByProductAndLocationAsync(productId, location.Id, Arg.Any<CancellationToken>()).Returns(item);

        var handler = new AdjustStockCommandHandler(inventoryItemRepository, locationRepository, eventPublisher);
        var result = await handler.Handle(new AdjustStockCommand(productId, location.Id, -8, "damage"), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(12, result.Value!.OnHand);
        Assert.Equal(5, result.Value.Reserved);
        await inventoryItemRepository.DidNotReceive().AddAsync(Arg.Any<InventoryItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DeltaWouldGoNegative_ReturnsFailure()
    {
        var inventoryItemRepository = Substitute.For<IInventoryItemRepository>();
        var locationRepository = Substitute.For<ILocationRepository>();
        var eventPublisher = Substitute.For<IEventPublisher>();

        var productId = Guid.NewGuid();
        var location = new Location { Id = Guid.NewGuid(), Name = "A", Code = "WH-A", IsActive = true };
        var item = new InventoryItem { Id = Guid.NewGuid(), ProductId = productId, LocationId = location.Id, OnHand = 5, Reserved = 0 };
        locationRepository.GetByIdAsync(location.Id, Arg.Any<CancellationToken>()).Returns(location);
        inventoryItemRepository.GetByProductAndLocationAsync(productId, location.Id, Arg.Any<CancellationToken>()).Returns(item);

        var handler = new AdjustStockCommandHandler(inventoryItemRepository, locationRepository, eventPublisher);
        var result = await handler.Handle(new AdjustStockCommand(productId, location.Id, -10, "correction"), CancellationToken.None);

        Assert.False(result.Succeeded);
        await eventPublisher.DidNotReceive().PublishStockAdjustedAsync(Arg.Any<StockAdjustedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownLocation_ReturnsFailure()
    {
        var inventoryItemRepository = Substitute.For<IInventoryItemRepository>();
        var locationRepository = Substitute.For<ILocationRepository>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        locationRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Location?)null);

        var handler = new AdjustStockCommandHandler(inventoryItemRepository, locationRepository, eventPublisher);
        var result = await handler.Handle(new AdjustStockCommand(Guid.NewGuid(), Guid.NewGuid(), 5, "restock"), CancellationToken.None);

        Assert.False(result.Succeeded);
    }
}
