using InventoryService.Application.Commands;
using InventoryService.Application.Handlers;
using InventoryService.Application.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Events;
using NSubstitute;

namespace InventoryService.UnitTests.Handlers;

public class CommitReservationCommandHandlerTests
{
    [Fact]
    public async Task Handle_ActiveReservation_CommitsAndDecrementsOnHandAndReserved()
    {
        var stockReservationRepository = Substitute.For<IStockReservationRepository>();
        var inventoryItemRepository = Substitute.For<IInventoryItemRepository>();
        var eventPublisher = Substitute.For<IEventPublisher>();

        var productId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var reservation = new StockReservation
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            LocationId = locationId,
            Quantity = 10,
            Status = ReservationStatus.Active
        };
        var item = new InventoryItem { Id = Guid.NewGuid(), ProductId = productId, LocationId = locationId, OnHand = 20, Reserved = 10 };
        stockReservationRepository.GetByIdAsync(reservation.Id, Arg.Any<CancellationToken>()).Returns(reservation);
        inventoryItemRepository.GetByProductAndLocationAsync(productId, locationId, Arg.Any<CancellationToken>()).Returns(item);

        var handler = new CommitReservationCommandHandler(stockReservationRepository, inventoryItemRepository, eventPublisher);
        var result = await handler.Handle(new CommitReservationCommand(reservation.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Committed", result.Value!.Status);
        Assert.Equal(10, item.OnHand);
        Assert.Equal(0, item.Reserved);
        await eventPublisher.Received(1).PublishReservationCommittedAsync(Arg.Any<ReservationCommittedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyCommittedReservation_ReturnsFailure()
    {
        var stockReservationRepository = Substitute.For<IStockReservationRepository>();
        var inventoryItemRepository = Substitute.For<IInventoryItemRepository>();
        var eventPublisher = Substitute.For<IEventPublisher>();

        var reservation = new StockReservation { Id = Guid.NewGuid(), Status = ReservationStatus.Committed, Quantity = 5 };
        stockReservationRepository.GetByIdAsync(reservation.Id, Arg.Any<CancellationToken>()).Returns(reservation);

        var handler = new CommitReservationCommandHandler(stockReservationRepository, inventoryItemRepository, eventPublisher);
        var result = await handler.Handle(new CommitReservationCommand(reservation.Id), CancellationToken.None);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Handle_UnknownReservation_ReturnsFailure()
    {
        var stockReservationRepository = Substitute.For<IStockReservationRepository>();
        var inventoryItemRepository = Substitute.For<IInventoryItemRepository>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        stockReservationRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((StockReservation?)null);

        var handler = new CommitReservationCommandHandler(stockReservationRepository, inventoryItemRepository, eventPublisher);
        var result = await handler.Handle(new CommitReservationCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
    }
}
