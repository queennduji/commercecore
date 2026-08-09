using OrderService.Application.Commands;
using OrderService.Application.Handlers;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using NSubstitute;

namespace OrderService.UnitTests.Handlers;

public class CheckoutCommandHandlerTests
{
    [Fact]
    public async Task Handle_CartWithAvailableStock_ReservesEachLineCreatesOrderAndClearsCart()
    {
        var orderRepository = Substitute.For<IOrderRepository>();
        var cartServiceClient = Substitute.For<ICartServiceClient>();
        var inventoryServiceClient = Substitute.For<IInventoryServiceClient>();
        var eventPublisher = Substitute.For<IEventPublisher>();

        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();

        cartServiceClient.GetCartAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new CartSnapshot(userId, [new CartLineSnapshot(productId, "SKU-1", "Widget", 9.99m, 2)]));
        inventoryServiceClient.GetStockAsync(productId, Arg.Any<CancellationToken>())
            .Returns([new LocationStockSnapshot(locationId, 10)]);
        inventoryServiceClient.ReserveAsync(productId, locationId, 2, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(reservationId);

        var handler = new CheckoutCommandHandler(orderRepository, cartServiceClient, inventoryServiceClient, eventPublisher);
        var result = await handler.Handle(new CheckoutCommand(userId, "123 Main St"), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(userId, result.Value!.UserId);
        Assert.Equal("Pending", result.Value.Status);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(productId, item.ProductId);
        Assert.Equal(locationId, item.LocationId);
        Assert.Equal(19.98m, result.Value.Subtotal);

        await orderRepository.Received(1).AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
        await orderRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await cartServiceClient.Received(1).ClearCartAsync(userId, Arg.Any<CancellationToken>());
        await eventPublisher.Received(1).PublishOrderCreatedAsync(Arg.Any<Domain.Events.OrderCreatedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyCart_ReturnsFailureWithoutReserving()
    {
        var orderRepository = Substitute.For<IOrderRepository>();
        var cartServiceClient = Substitute.For<ICartServiceClient>();
        var inventoryServiceClient = Substitute.For<IInventoryServiceClient>();
        var eventPublisher = Substitute.For<IEventPublisher>();

        var userId = Guid.NewGuid();
        cartServiceClient.GetCartAsync(userId, Arg.Any<CancellationToken>()).Returns(new CartSnapshot(userId, []));

        var handler = new CheckoutCommandHandler(orderRepository, cartServiceClient, inventoryServiceClient, eventPublisher);
        var result = await handler.Handle(new CheckoutCommand(userId, "123 Main St"), CancellationToken.None);

        Assert.False(result.Succeeded);
        await inventoryServiceClient.DidNotReceive().GetStockAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await orderRepository.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoLocationHasEnoughStockForSecondItem_ReleasesFirstReservationAndFails()
    {
        var orderRepository = Substitute.For<IOrderRepository>();
        var cartServiceClient = Substitute.For<ICartServiceClient>();
        var inventoryServiceClient = Substitute.For<IInventoryServiceClient>();
        var eventPublisher = Substitute.For<IEventPublisher>();

        var userId = Guid.NewGuid();
        var productA = Guid.NewGuid();
        var productB = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var reservationIdA = Guid.NewGuid();

        cartServiceClient.GetCartAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new CartSnapshot(userId,
            [
                new CartLineSnapshot(productA, "SKU-A", "Widget A", 9.99m, 1),
                new CartLineSnapshot(productB, "SKU-B", "Widget B", 5m, 100)
            ]));

        inventoryServiceClient.GetStockAsync(productA, Arg.Any<CancellationToken>())
            .Returns([new LocationStockSnapshot(locationId, 10)]);
        inventoryServiceClient.ReserveAsync(productA, locationId, 1, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(reservationIdA);

        inventoryServiceClient.GetStockAsync(productB, Arg.Any<CancellationToken>())
            .Returns([new LocationStockSnapshot(locationId, 3)]); // not enough for quantity 100

        var handler = new CheckoutCommandHandler(orderRepository, cartServiceClient, inventoryServiceClient, eventPublisher);
        var result = await handler.Handle(new CheckoutCommand(userId, "123 Main St"), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains(productB.ToString()));
        await inventoryServiceClient.Received(1).ReleaseAsync(reservationIdA, Arg.Any<CancellationToken>());
        await orderRepository.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
        await cartServiceClient.DidNotReceive().ClearCartAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownCart_ReturnsFailure()
    {
        var orderRepository = Substitute.For<IOrderRepository>();
        var cartServiceClient = Substitute.For<ICartServiceClient>();
        var inventoryServiceClient = Substitute.For<IInventoryServiceClient>();
        var eventPublisher = Substitute.For<IEventPublisher>();

        var userId = Guid.NewGuid();
        cartServiceClient.GetCartAsync(userId, Arg.Any<CancellationToken>()).Returns((CartSnapshot?)null);

        var handler = new CheckoutCommandHandler(orderRepository, cartServiceClient, inventoryServiceClient, eventPublisher);
        var result = await handler.Handle(new CheckoutCommand(userId, "123 Main St"), CancellationToken.None);

        Assert.False(result.Succeeded);
    }
}
