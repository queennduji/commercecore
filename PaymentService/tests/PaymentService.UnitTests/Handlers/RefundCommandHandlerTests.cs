using PaymentService.Application.Commands;
using PaymentService.Application.Handlers;
using PaymentService.Application.Interfaces;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Events;
using NSubstitute;

namespace PaymentService.UnitTests.Handlers;

public class RefundCommandHandlerTests
{
    [Fact]
    public async Task Handle_SucceededPaymentExists_RefundsAndPublishesEvent()
    {
        var paymentRepository = Substitute.For<IPaymentRepository>();
        var paymentGateway = Substitute.For<IPaymentGateway>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var orderId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            UserId = Guid.NewGuid(),
            Amount = 50m,
            Currency = "usd",
            Status = PaymentStatus.Succeeded,
            ProviderReference = "pi_456",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        paymentRepository.GetLatestSucceededByOrderIdAsync(orderId, Arg.Any<CancellationToken>()).Returns(payment);
        paymentGateway.RefundAsync("pi_456", Arg.Any<CancellationToken>()).Returns(new GatewayRefundResult(true, "re_789", null));

        var handler = new RefundCommandHandler(paymentRepository, paymentGateway, eventPublisher);
        var result = await handler.Handle(new RefundCommand(orderId), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Refunded", result.Value!.Status);
        await paymentRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await eventPublisher.Received(1).PublishPaymentRefundedAsync(
            Arg.Is<PaymentRefundedEvent>(e => e != null && e.OrderId == orderId && e.Amount == 50m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoSucceededPaymentForOrder_ReturnsFailure()
    {
        var paymentRepository = Substitute.For<IPaymentRepository>();
        var paymentGateway = Substitute.For<IPaymentGateway>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var orderId = Guid.NewGuid();
        paymentRepository.GetLatestSucceededByOrderIdAsync(orderId, Arg.Any<CancellationToken>()).Returns((Payment?)null);

        var handler = new RefundCommandHandler(paymentRepository, paymentGateway, eventPublisher);
        var result = await handler.Handle(new RefundCommand(orderId), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("No successful payment", result.Errors.Single());
        await paymentGateway.DidNotReceive().RefundAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GatewayRefundFails_ReturnsFailureAndDoesNotTransition()
    {
        var paymentRepository = Substitute.For<IPaymentRepository>();
        var paymentGateway = Substitute.For<IPaymentGateway>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var orderId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            UserId = Guid.NewGuid(),
            Amount = 50m,
            Currency = "usd",
            Status = PaymentStatus.Succeeded,
            ProviderReference = "pi_456",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        paymentRepository.GetLatestSucceededByOrderIdAsync(orderId, Arg.Any<CancellationToken>()).Returns(payment);
        paymentGateway.RefundAsync("pi_456", Arg.Any<CancellationToken>()).Returns(new GatewayRefundResult(false, null, "Charge already refunded."));

        var handler = new RefundCommandHandler(paymentRepository, paymentGateway, eventPublisher);
        var result = await handler.Handle(new RefundCommand(orderId), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        await paymentRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await eventPublisher.DidNotReceive().PublishPaymentRefundedAsync(Arg.Any<PaymentRefundedEvent>(), Arg.Any<CancellationToken>());
    }
}
