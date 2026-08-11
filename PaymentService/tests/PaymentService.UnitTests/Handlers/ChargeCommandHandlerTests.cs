using PaymentService.Application.Commands;
using PaymentService.Application.Handlers;
using PaymentService.Application.Interfaces;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Events;
using NSubstitute;

namespace PaymentService.UnitTests.Handlers;

public class ChargeCommandHandlerTests
{
    [Fact]
    public async Task Handle_GatewayCharges_RecordsSucceededPaymentAndPublishesEvent()
    {
        var paymentRepository = Substitute.For<IPaymentRepository>();
        var paymentGateway = Substitute.For<IPaymentGateway>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        paymentGateway.ChargeAsync(100m, "usd", "pm_card_visa", $"Order {orderId}", Arg.Any<CancellationToken>())
            .Returns(new GatewayChargeResult(true, "pi_123", null));

        var handler = new ChargeCommandHandler(paymentRepository, paymentGateway, eventPublisher);
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
        paymentGateway.ChargeAsync(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GatewayChargeResult(false, null, "Your card was declined."));

        var handler = new ChargeCommandHandler(paymentRepository, paymentGateway, eventPublisher);
        var result = await handler.Handle(new ChargeCommand(orderId, userId, 100m, "usd", "pm_card_visa_chargeDeclined"), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("declined", result.Errors.Single(), StringComparison.OrdinalIgnoreCase);
        await paymentRepository.Received(1).AddAsync(
            Arg.Is<Payment>(p => p != null && p.Status == PaymentStatus.Failed && p.FailureReason == "Your card was declined."),
            Arg.Any<CancellationToken>());
        await eventPublisher.Received(1).PublishPaymentFailedAsync(Arg.Any<PaymentFailedEvent>(), Arg.Any<CancellationToken>());
        await eventPublisher.DidNotReceive().PublishPaymentSucceededAsync(Arg.Any<PaymentSucceededEvent>(), Arg.Any<CancellationToken>());
    }
}
