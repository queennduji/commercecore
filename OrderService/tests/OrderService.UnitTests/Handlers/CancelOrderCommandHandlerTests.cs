using OrderService.Application.Commands;
using OrderService.Application.Handlers;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using NSubstitute;

namespace OrderService.UnitTests.Handlers;

public class CancelOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_PendingOrder_ReleasesReservationsAndCancels()
    {
        var orderRepository = Substitute.For<IOrderRepository>();
        var inventoryServiceClient = Substitute.For<IInventoryServiceClient>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var userId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = OrderStatus.Pending,
            ShippingAddress = "addr",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = [new OrderItem { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), Sku = "SKU-1", Name = "Widget", UnitPrice = 5m, Quantity = 1, LocationId = Guid.NewGuid(), ReservationId = reservationId }]
        };
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new CancelOrderCommandHandler(orderRepository, inventoryServiceClient, eventPublisher);
        var result = await handler.Handle(new CancelOrderCommand(order.Id, userId), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Cancelled", result.Value!.Status);
        await inventoryServiceClient.Received(1).ReleaseAsync(reservationId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShippedOrder_ReturnsFailure()
    {
        var orderRepository = Substitute.For<IOrderRepository>();
        var inventoryServiceClient = Substitute.For<IInventoryServiceClient>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var userId = Guid.NewGuid();
        var order = new Order { Id = Guid.NewGuid(), UserId = userId, Status = OrderStatus.Shipped, ShippingAddress = "addr", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new CancelOrderCommandHandler(orderRepository, inventoryServiceClient, eventPublisher);
        var result = await handler.Handle(new CancelOrderCommand(order.Id, userId), CancellationToken.None);

        Assert.False(result.Succeeded);
        await inventoryServiceClient.DidNotReceive().ReleaseAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
