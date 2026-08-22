using OrderService.Application.Commands;
using OrderService.Application.Handlers;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using NSubstitute;

namespace OrderService.UnitTests.Handlers;

public class RefundOrderCommandHandlerTests
{
    // No ownership check at this layer by design - see RefundOrderCommand's doc comment.
    // Authorization is enforced at OrdersController via [Authorize(Roles = "Admin")] instead,
    // which isn't something a handler-level unit test exercises (that's framework middleware,
    // not application code) - covered live in the deployment smoke test instead.

    [Theory]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    public async Task Handle_RefundableStatus_TransitionsToRefunded(OrderStatus status)
    {
        var orderRepository = Substitute.For<IOrderRepository>();
        var paymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var userId = Guid.NewGuid();
        var order = new Order { Id = Guid.NewGuid(), UserId = userId, Status = status, ShippingAddress = "addr", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        paymentServiceClient.RefundAsync(order.Id, Arg.Any<CancellationToken>()).Returns(new PaymentResult(true, null));

        var handler = new RefundOrderCommandHandler(orderRepository, paymentServiceClient, eventPublisher);
        var result = await handler.Handle(new RefundOrderCommand(order.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Refunded", result.Value!.Status);
    }

    [Fact]
    public async Task Handle_PendingOrder_ReturnsFailure()
    {
        var orderRepository = Substitute.For<IOrderRepository>();
        var paymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var order = new Order { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Status = OrderStatus.Pending, ShippingAddress = "addr", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new RefundOrderCommandHandler(orderRepository, paymentServiceClient, eventPublisher);
        var result = await handler.Handle(new RefundOrderCommand(order.Id), CancellationToken.None);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Handle_GatewayRefundFails_ReturnsFailureAndDoesNotTransition()
    {
        var orderRepository = Substitute.For<IOrderRepository>();
        var paymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var order = new Order { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Status = OrderStatus.Paid, ShippingAddress = "addr", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        paymentServiceClient.RefundAsync(order.Id, Arg.Any<CancellationToken>()).Returns(new PaymentResult(false, "No successful payment found for this order."));

        var handler = new RefundOrderCommandHandler(orderRepository, paymentServiceClient, eventPublisher);
        var result = await handler.Handle(new RefundOrderCommand(order.Id), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OrderStatus.Paid, order.Status);
        await eventPublisher.DidNotReceive().PublishOrderRefundedAsync(Arg.Any<Domain.Events.OrderRefundedEvent>(), Arg.Any<CancellationToken>());
    }
}
