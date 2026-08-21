using OrderService.Application.Commands;
using OrderService.Application.Handlers;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using NSubstitute;

namespace OrderService.UnitTests.Handlers;

public class MarkOrderPaidCommandHandlerTests
{
    [Fact]
    public async Task Handle_PendingOrderOwnedByCaller_TransitionsToPaid()
    {
        var orderRepository = Substitute.For<IOrderRepository>();
        var paymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var userId = Guid.NewGuid();
        var order = new Order { Id = Guid.NewGuid(), UserId = userId, Status = OrderStatus.Pending, ShippingAddress = "addr", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        paymentServiceClient.ChargeAsync(order.Id, Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentResult(true, null));

        var handler = new MarkOrderPaidCommandHandler(orderRepository, paymentServiceClient, eventPublisher);
        var result = await handler.Handle(new MarkOrderPaidCommand(order.Id, userId, "pm_card_visa"), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Paid", result.Value!.Status);
        await eventPublisher.Received(1).PublishOrderPaidAsync(Arg.Any<Domain.Events.OrderPaidEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DifferentOwner_ReturnsNotFoundStyleFailure()
    {
        var orderRepository = Substitute.For<IOrderRepository>();
        var paymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var order = new Order { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Status = OrderStatus.Pending, ShippingAddress = "addr", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new MarkOrderPaidCommandHandler(orderRepository, paymentServiceClient, eventPublisher);
        var result = await handler.Handle(new MarkOrderPaidCommand(order.Id, Guid.NewGuid(), "pm_card_visa"), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("not found", result.Errors.Single(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_AlreadyPaidOrder_ReturnsFailure()
    {
        var orderRepository = Substitute.For<IOrderRepository>();
        var paymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var userId = Guid.NewGuid();
        var order = new Order { Id = Guid.NewGuid(), UserId = userId, Status = OrderStatus.Paid, ShippingAddress = "addr", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new MarkOrderPaidCommandHandler(orderRepository, paymentServiceClient, eventPublisher);
        var result = await handler.Handle(new MarkOrderPaidCommand(order.Id, userId, "pm_card_visa"), CancellationToken.None);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Handle_PaymentDeclined_ReturnsFailureAndDoesNotTransition()
    {
        var orderRepository = Substitute.For<IOrderRepository>();
        var paymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var userId = Guid.NewGuid();
        var order = new Order { Id = Guid.NewGuid(), UserId = userId, Status = OrderStatus.Pending, ShippingAddress = "addr", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        paymentServiceClient.ChargeAsync(order.Id, Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentResult(false, "Your card was declined."));

        var handler = new MarkOrderPaidCommandHandler(orderRepository, paymentServiceClient, eventPublisher);
        var result = await handler.Handle(new MarkOrderPaidCommand(order.Id, userId, "pm_card_visa_chargeDeclined"), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OrderStatus.Pending, order.Status);
        await eventPublisher.DidNotReceive().PublishOrderPaidAsync(Arg.Any<Domain.Events.OrderPaidEvent>(), Arg.Any<CancellationToken>());
    }
}
