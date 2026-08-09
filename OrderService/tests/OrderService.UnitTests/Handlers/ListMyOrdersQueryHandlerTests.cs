using OrderService.Application.Handlers;
using OrderService.Application.Interfaces;
using OrderService.Application.Queries;
using OrderService.Domain.Entities;
using NSubstitute;

namespace OrderService.UnitTests.Handlers;

public class ListMyOrdersQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsPagedOrdersForCaller()
    {
        var orderRepository = Substitute.For<IOrderRepository>();
        var userId = Guid.NewGuid();
        var order = new Order { Id = Guid.NewGuid(), UserId = userId, Status = OrderStatus.Pending, ShippingAddress = "addr", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        orderRepository.ListByUserIdAsync(userId, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<Order> { order }, 1));

        var handler = new ListMyOrdersQueryHandler(orderRepository);
        var result = await handler.Handle(new ListMyOrdersQuery(userId, 1, 20), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Single(result.Value!.Items);
        Assert.Equal(1, result.Value.TotalCount);
    }
}
