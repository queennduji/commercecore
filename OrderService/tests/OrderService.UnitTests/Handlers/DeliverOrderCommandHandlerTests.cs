using OrderService.Application.Commands;
using OrderService.Application.Handlers;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using NSubstitute;

namespace OrderService.UnitTests.Handlers;

public class DeliverOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShippedOrder_TransitionsToDelivered()
    {
        var orderRepository = Substitute.For<IOrderRepository>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var order = new Order { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Status = OrderStatus.Shipped, ShippingAddress = "addr", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new DeliverOrderCommandHandler(orderRepository, eventPublisher);
        var result = await handler.Handle(new DeliverOrderCommand(order.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Delivered", result.Value!.Status);
        await eventPublisher.Received(1).PublishOrderDeliveredAsync(Arg.Any<Domain.Events.OrderDeliveredEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PaidOrder_ReturnsFailure()
    {
        var orderRepository = Substitute.For<IOrderRepository>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var order = new Order { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Status = OrderStatus.Paid, ShippingAddress = "addr", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new DeliverOrderCommandHandler(orderRepository, eventPublisher);
        var result = await handler.Handle(new DeliverOrderCommand(order.Id), CancellationToken.None);

        Assert.False(result.Succeeded);
    }
}
