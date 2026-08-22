using OrderService.Application.Commands;
using OrderService.Application.Handlers;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using NSubstitute;

namespace OrderService.UnitTests.Handlers;

public class RefundOrderCommandHandlerTests
{
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
        var result = await handler.Handle(new RefundOrderCommand(order.Id, userId), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Refunded", result.Value!.Status);
    }

    [Fact]
    public async Task Handle_PendingOrder_ReturnsFailure()
    {
        var orderRepository = Substitute.For<IOrderRepository>();
        var paymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var userId = Guid.NewGuid();
        var order = new Order { Id = Guid.NewGuid(), UserId = userId, Status = OrderStatus.Pending, ShippingAddress = "addr", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new RefundOrderCommandHandler(orderRepository, paymentServiceClient, eventPublisher);
        var result = await handler.Handle(new RefundOrderCommand(order.Id, userId), CancellationToken.None);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Handle_GatewayRefundFails_ReturnsFailureAndDoesNotTransition()
    {
        var orderRepository = Substitute.For<IOrderRepository>();
        var paymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var userId = Guid.NewGuid();
        var order = new Order { Id = Guid.NewGuid(), UserId = userId, Status = OrderStatus.Paid, ShippingAddress = "addr", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        paymentServiceClient.RefundAsync(order.Id, Arg.Any<CancellationToken>()).Returns(new PaymentResult(false, "No successful payment found for this order."));

        var handler = new RefundOrderCommandHandler(orderRepository, paymentServiceClient, eventPublisher);
        var result = await handler.Handle(new RefundOrderCommand(order.Id, userId), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OrderStatus.Paid, order.Status);
        await eventPublisher.DidNotReceive().PublishOrderRefundedAsync(Arg.Any<Domain.Events.OrderRefundedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DifferentOwner_ReturnsNotFoundStyleFailure()
    {
        // The actual vulnerability this command's UserId parameter fixes: without this check, any
        // authenticated caller could refund an order that isn't theirs just by knowing its id.
        var orderRepository = Substitute.For<IOrderRepository>();
        var paymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var order = new Order { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Status = OrderStatus.Paid, ShippingAddress = "addr", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new RefundOrderCommandHandler(orderRepository, paymentServiceClient, eventPublisher);
        var result = await handler.Handle(new RefundOrderCommand(order.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("not found", result.Errors.Single(), StringComparison.OrdinalIgnoreCase);
        await paymentServiceClient.DidNotReceive().RefundAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
