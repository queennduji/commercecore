using OrderService.Application.Commands;
using OrderService.Application.Handlers;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using NSubstitute;

namespace OrderService.UnitTests.Handlers;

public class ShipOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_PaidOrder_CommitsReservationsAndShips()
    {
        var orderRepository = Substitute.For<IOrderRepository>();
        var inventoryServiceClient = Substitute.For<IInventoryServiceClient>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var reservationId = Guid.NewGuid();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = OrderStatus.Paid,
            ShippingAddress = "addr",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = [new OrderItem { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), Sku = "SKU-1", Name = "Widget", UnitPrice = 5m, Quantity = 1, LocationId = Guid.NewGuid(), ReservationId = reservationId }]
        };
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        inventoryServiceClient.CommitAsync(reservationId, Arg.Any<CancellationToken>()).Returns(true);

        var handler = new ShipOrderCommandHandler(orderRepository, inventoryServiceClient, eventPublisher);
        var result = await handler.Handle(new ShipOrderCommand(order.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Shipped", result.Value!.Status);
        await eventPublisher.Received(1).PublishOrderShippedAsync(Arg.Any<Domain.Events.OrderShippedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CommitFails_ReturnsFailureAndOrderStaysPaid()
    {
        var orderRepository = Substitute.For<IOrderRepository>();
        var inventoryServiceClient = Substitute.For<IInventoryServiceClient>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var reservationId = Guid.NewGuid();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = OrderStatus.Paid,
            ShippingAddress = "addr",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = [new OrderItem { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), Sku = "SKU-1", Name = "Widget", UnitPrice = 5m, Quantity = 1, LocationId = Guid.NewGuid(), ReservationId = reservationId }]
        };
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        inventoryServiceClient.CommitAsync(reservationId, Arg.Any<CancellationToken>()).Returns(false);

        var handler = new ShipOrderCommandHandler(orderRepository, inventoryServiceClient, eventPublisher);
        var result = await handler.Handle(new ShipOrderCommand(order.Id), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OrderStatus.Paid, order.Status);
        await eventPublisher.DidNotReceive().PublishOrderShippedAsync(Arg.Any<Domain.Events.OrderShippedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PendingOrder_ReturnsFailure()
    {
        var orderRepository = Substitute.For<IOrderRepository>();
        var inventoryServiceClient = Substitute.For<IInventoryServiceClient>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var order = new Order { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Status = OrderStatus.Pending, ShippingAddress = "addr", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new ShipOrderCommandHandler(orderRepository, inventoryServiceClient, eventPublisher);
        var result = await handler.Handle(new ShipOrderCommand(order.Id), CancellationToken.None);

        Assert.False(result.Succeeded);
    }
}
