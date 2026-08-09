using OrderService.Application.Handlers;
using OrderService.Application.Interfaces;
using OrderService.Application.Queries;
using OrderService.Domain.Entities;
using NSubstitute;

namespace OrderService.UnitTests.Handlers;

public class GetOrderQueryHandlerTests
{
    [Fact]
    public async Task Handle_OwnedOrder_ReturnsDto()
    {
        var orderRepository = Substitute.For<IOrderRepository>();
        var userId = Guid.NewGuid();
        var order = new Order { Id = Guid.NewGuid(), UserId = userId, Status = OrderStatus.Pending, ShippingAddress = "addr", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new GetOrderQueryHandler(orderRepository);
        var result = await handler.Handle(new GetOrderQuery(order.Id, userId), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(order.Id, result.Value!.Id);
    }

    [Fact]
    public async Task Handle_DifferentOwner_ReturnsNotFoundStyleFailure()
    {
        var orderRepository = Substitute.For<IOrderRepository>();
        var order = new Order { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Status = OrderStatus.Pending, ShippingAddress = "addr", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new GetOrderQueryHandler(orderRepository);
        var result = await handler.Handle(new GetOrderQuery(order.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Handle_UnknownOrder_ReturnsFailure()
    {
        var orderRepository = Substitute.For<IOrderRepository>();
        orderRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Order?)null);

        var handler = new GetOrderQueryHandler(orderRepository);
        var result = await handler.Handle(new GetOrderQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
    }
}
