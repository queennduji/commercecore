using PaymentService.Application.Commands;
using PaymentService.Application.Handlers;
using PaymentService.Application.Interfaces;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Events;
using NSubstitute;

namespace PaymentService.UnitTests.Handlers;

public class ChargeCommandHandlerTests
{
    /// <summary>A real IOrderChargeLock would serialize concurrently-racing requests; these tests
    /// exercise one request at a time, so a lock that always grants immediately is the correct
    /// fake here - the locking behavior itself belongs in an integration test that can actually
    /// run two requests concurrently, not a unit test around this single-threaded handler.</summary>
    private static IOrderChargeLock CreateNoOpLock()
    {
        var orderChargeLock = Substitute.For<IOrderChargeLock>();
        orderChargeLock.AcquireAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(NoOpAsyncDisposable.Instance);
        return orderChargeLock;
    }

    private sealed class NoOpAsyncDisposable : IAsyncDisposable
    {
        public static readonly NoOpAsyncDisposable Instance = new();
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Handle_GatewayCharges_RecordsSucceededPaymentAndPublishesEvent()
    {
        var paymentRepository = Substitute.For<IPaymentRepository>();
        var paymentGateway = Substitute.For<IPaymentGateway>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        paymentGateway.ChargeAsync(100m, "usd", "pm_card_visa", $"Order {orderId}", orderId.ToString(), Arg.Any<CancellationToken>())
            .Returns(new GatewayChargeResult(true, "pi_123", null));

        var handler = new ChargeCommandHandler(paymentRepository, paymentGateway, eventPublisher, CreateNoOpLock());
        var result = await handler.Handle(new ChargeCommand(orderId, userId, 100m, "usd", "pm_card_visa"), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Succeeded", result.Value!.Status);
        Assert.Equal("pi_123", result.Value.ProviderReference);
        await paymentRepository.Received(1).AddAsync(
            Arg.Is<Payment>(p => p != null && p.OrderId == orderId && p.UserId == userId && p.Status == PaymentStatus.Succeeded && p.ProviderReference == "pi_123"),
            Arg.Any<CancellationToken>());
        await paymentRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await eventPublisher.Received(1).PublishPaymentSucceededAsync(
            Arg.Is<PaymentSucceededEvent>(e => e != null && e.OrderId == orderId && e.UserId == userId && e.Amount == 100m),
            Arg.Any<CancellationToken>());
        await eventPublisher.DidNotReceive().PublishPaymentFailedAsync(Arg.Any<PaymentFailedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GatewayDeclines_RecordsFailedPaymentAndPublishesFailureEvent()
    {
        var paymentRepository = Substitute.For<IPaymentRepository>();
        var paymentGateway = Substitute.For<IPaymentGateway>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        paymentGateway.ChargeAsync(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GatewayChargeResult(false, null, "Your card was declined."));

        var handler = new ChargeCommandHandler(paymentRepository, paymentGateway, eventPublisher, CreateNoOpLock());
        var result = await handler.Handle(new ChargeCommand(orderId, userId, 100m, "usd", "pm_card_visa_chargeDeclined"), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("declined", result.Errors.Single(), StringComparison.OrdinalIgnoreCase);
        await paymentRepository.Received(1).AddAsync(
            Arg.Is<Payment>(p => p != null && p.Status == PaymentStatus.Failed && p.FailureReason == "Your card was declined."),
            Arg.Any<CancellationToken>());
        await eventPublisher.Received(1).PublishPaymentFailedAsync(Arg.Any<PaymentFailedEvent>(), Arg.Any<CancellationToken>());
        await eventPublisher.DidNotReceive().PublishPaymentSucceededAsync(Arg.Any<PaymentSucceededEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderAlreadyHasSucceededPayment_ReturnsExistingPaymentWithoutChargingAgain()
    {
        // Guards a duplicate call reaching this handler for an order that already succeeded (e.g.
        // a retry firing after a slow-but-successful first attempt) - it must short-circuit before
        // ever touching the gateway or the repository/event-publisher write paths, not just return
        // the same-looking result via a second real charge.
        var paymentRepository = Substitute.For<IPaymentRepository>();
        var paymentGateway = Substitute.For<IPaymentGateway>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existingPayment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            UserId = userId,
            Amount = 100m,
            Currency = "usd",
            Status = PaymentStatus.Succeeded,
            ProviderReference = "pi_123",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        paymentRepository.GetLatestSucceededByOrderIdAsync(orderId, Arg.Any<CancellationToken>())
            .Returns(existingPayment);

        var handler = new ChargeCommandHandler(paymentRepository, paymentGateway, eventPublisher, CreateNoOpLock());
        var result = await handler.Handle(new ChargeCommand(orderId, userId, 100m, "usd", "pm_card_visa"), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("pi_123", result.Value!.ProviderReference);
        await paymentGateway.DidNotReceive().ChargeAsync(
            Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await paymentRepository.DidNotReceive().AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
        await eventPublisher.DidNotReceive().PublishPaymentSucceededAsync(Arg.Any<PaymentSucceededEvent>(), Arg.Any<CancellationToken>());
    }
}
