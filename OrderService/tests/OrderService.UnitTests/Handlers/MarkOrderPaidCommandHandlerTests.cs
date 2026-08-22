using OrderService.Application.Commands;
using OrderService.Application.Handlers;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using NSubstitute;

namespace OrderService.UnitTests.Handlers;

public class MarkOrderPaidCommandHandlerTests
{
    /// <summary>A real IOrderPaymentLock would serialize concurrently-racing requests; these tests
    /// exercise one request at a time, so a lock that always grants immediately is the correct
    /// fake here - the locking behavior itself belongs in an integration/concurrency test, not a
    /// unit test around this single-threaded handler.</summary>
    private static IOrderPaymentLock CreateNoOpLock()
    {
        var orderPaymentLock = Substitute.For<IOrderPaymentLock>();
        orderPaymentLock.AcquireAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(NoOpAsyncDisposable.Instance);
        return orderPaymentLock;
    }

    private sealed class NoOpAsyncDisposable : IAsyncDisposable
    {
        public static readonly NoOpAsyncDisposable Instance = new();
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

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

        var handler = new MarkOrderPaidCommandHandler(orderRepository, paymentServiceClient, eventPublisher, CreateNoOpLock());
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

        var handler = new MarkOrderPaidCommandHandler(orderRepository, paymentServiceClient, eventPublisher, CreateNoOpLock());
        var result = await handler.Handle(new MarkOrderPaidCommand(order.Id, Guid.NewGuid(), "pm_card_visa"), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("not found", result.Errors.Single(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_AlreadyPaidOrder_ReturnsSuccessIdempotentlyWithoutChargingAgain()
    {
        // A duplicate request reaching this handler for an already-Paid order (e.g. one that
        // waited on the lock while an earlier request finished) must converge on success - the
        // order genuinely is paid, which is what the caller asked for - rather than surfacing an
        // error for what is, from the caller's point of view, exactly the outcome they wanted.
        var orderRepository = Substitute.For<IOrderRepository>();
        var paymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var userId = Guid.NewGuid();
        var order = new Order { Id = Guid.NewGuid(), UserId = userId, Status = OrderStatus.Paid, ShippingAddress = "addr", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new MarkOrderPaidCommandHandler(orderRepository, paymentServiceClient, eventPublisher, CreateNoOpLock());
        var result = await handler.Handle(new MarkOrderPaidCommand(order.Id, userId, "pm_card_visa"), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Paid", result.Value!.Status);
        await paymentServiceClient.DidNotReceive().ChargeAsync(
            Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await eventPublisher.DidNotReceive().PublishOrderPaidAsync(Arg.Any<Domain.Events.OrderPaidEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonPendingNonPaidOrder_ReturnsFailure()
    {
        // A genuinely invalid transition (e.g. trying to pay a Cancelled or Refunded order) is
        // still a real error, unlike the already-Paid case above - only "Paid" is the idempotent
        // no-op target state for this specific command.
        var orderRepository = Substitute.For<IOrderRepository>();
        var paymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var userId = Guid.NewGuid();
        var order = new Order { Id = Guid.NewGuid(), UserId = userId, Status = OrderStatus.Cancelled, ShippingAddress = "addr", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new MarkOrderPaidCommandHandler(orderRepository, paymentServiceClient, eventPublisher, CreateNoOpLock());
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

        var handler = new MarkOrderPaidCommandHandler(orderRepository, paymentServiceClient, eventPublisher, CreateNoOpLock());
        var result = await handler.Handle(new MarkOrderPaidCommand(order.Id, userId, "pm_card_visa_chargeDeclined"), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(OrderStatus.Pending, order.Status);
        await eventPublisher.DidNotReceive().PublishOrderPaidAsync(Arg.Any<Domain.Events.OrderPaidEvent>(), Arg.Any<CancellationToken>());
    }
}
