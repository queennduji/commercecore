using InventoryService.Application.Commands;
using InventoryService.Application.Handlers;
using InventoryService.Application.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Events;
using NSubstitute;

namespace InventoryService.UnitTests.Handlers;

public class ReleaseReservationCommandHandlerTests
{
    [Fact]
    public async Task Handle_ActiveReservation_ReleasesAndDecrementsReserved()
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

        var handler = new ReleaseReservationCommandHandler(stockReservationRepository, inventoryItemRepository, eventPublisher);
        var result = await handler.Handle(new ReleaseReservationCommand(reservation.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Released", result.Value!.Status);
        Assert.Equal(0, item.Reserved);
        Assert.Equal(20, item.OnHand);
        await eventPublisher.Received(1).PublishReservationReleasedAsync(Arg.Any<ReservationReleasedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyReleasedReservation_ReturnsFailure()
    {
        var stockReservationRepository = Substitute.For<IStockReservationRepository>();
        var inventoryItemRepository = Substitute.For<IInventoryItemRepository>();
        var eventPublisher = Substitute.For<IEventPublisher>();

        var reservation = new StockReservation { Id = Guid.NewGuid(), Status = ReservationStatus.Released, Quantity = 5 };
        stockReservationRepository.GetByIdAsync(reservation.Id, Arg.Any<CancellationToken>()).Returns(reservation);

        var handler = new ReleaseReservationCommandHandler(stockReservationRepository, inventoryItemRepository, eventPublisher);
        var result = await handler.Handle(new ReleaseReservationCommand(reservation.Id), CancellationToken.None);

        Assert.False(result.Succeeded);
        await eventPublisher.DidNotReceive().PublishReservationReleasedAsync(Arg.Any<ReservationReleasedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownReservation_ReturnsFailure()
    {
        var stockReservationRepository = Substitute.For<IStockReservationRepository>();
        var inventoryItemRepository = Substitute.For<IInventoryItemRepository>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        stockReservationRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((StockReservation?)null);

        var handler = new ReleaseReservationCommandHandler(stockReservationRepository, inventoryItemRepository, eventPublisher);
        var result = await handler.Handle(new ReleaseReservationCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
    }
}
