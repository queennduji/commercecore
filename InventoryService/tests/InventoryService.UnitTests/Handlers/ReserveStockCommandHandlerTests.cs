using InventoryService.Application.Commands;
using InventoryService.Application.Handlers;
using InventoryService.Application.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Events;
using NSubstitute;

namespace InventoryService.UnitTests.Handlers;

public class ReserveStockCommandHandlerTests
{
    [Fact]
    public async Task Handle_SufficientAvailableStock_CreatesReservation()
    {
        var inventoryItemRepository = Substitute.For<IInventoryItemRepository>();
        var stockReservationRepository = Substitute.For<IStockReservationRepository>();
        var eventPublisher = Substitute.For<IEventPublisher>();

        var productId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var item = new InventoryItem { Id = Guid.NewGuid(), ProductId = productId, LocationId = locationId, OnHand = 20, Reserved = 5 };
        inventoryItemRepository.GetByProductAndLocationAsync(productId, locationId, Arg.Any<CancellationToken>()).Returns(item);

        var handler = new ReserveStockCommandHandler(inventoryItemRepository, stockReservationRepository, eventPublisher);
        var result = await handler.Handle(new ReserveStockCommand(productId, locationId, 10, "order-123"), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(10, result.Value!.Quantity);
        Assert.Equal("Active", result.Value.Status);
        Assert.Equal(15, item.Reserved);
        await stockReservationRepository.Received(1).AddAsync(Arg.Any<StockReservation>(), Arg.Any<CancellationToken>());
        await eventPublisher.Received(1).PublishStockReservedAsync(
            Arg.Is<StockReservedEvent>(e => e.Quantity == 10 && e.ReferenceId == "order-123"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InsufficientAvailableStock_ReturnsFailure()
    {
        var inventoryItemRepository = Substitute.For<IInventoryItemRepository>();
        var stockReservationRepository = Substitute.For<IStockReservationRepository>();
        var eventPublisher = Substitute.For<IEventPublisher>();

        var productId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var item = new InventoryItem { Id = Guid.NewGuid(), ProductId = productId, LocationId = locationId, OnHand = 5, Reserved = 4 };
        inventoryItemRepository.GetByProductAndLocationAsync(productId, locationId, Arg.Any<CancellationToken>()).Returns(item);

        var handler = new ReserveStockCommandHandler(inventoryItemRepository, stockReservationRepository, eventPublisher);
        var result = await handler.Handle(new ReserveStockCommand(productId, locationId, 5, null), CancellationToken.None);

        Assert.False(result.Succeeded);
        await stockReservationRepository.DidNotReceive().AddAsync(Arg.Any<StockReservation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoInventoryRecordAtLocation_ReturnsFailure()
    {
        var inventoryItemRepository = Substitute.For<IInventoryItemRepository>();
        var stockReservationRepository = Substitute.For<IStockReservationRepository>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        inventoryItemRepository.GetByProductAndLocationAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((InventoryItem?)null);

        var handler = new ReserveStockCommandHandler(inventoryItemRepository, stockReservationRepository, eventPublisher);
        var result = await handler.Handle(new ReserveStockCommand(Guid.NewGuid(), Guid.NewGuid(), 1, null), CancellationToken.None);

        Assert.False(result.Succeeded);
    }
}
