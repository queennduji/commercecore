using PaymentService.Application.Handlers;
using PaymentService.Application.Interfaces;
using PaymentService.Application.Queries;
using PaymentService.Domain.Entities;
using NSubstitute;

namespace PaymentService.UnitTests.Handlers;

public class ListPaymentsByOrderQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsOnlyPaymentsOwnedByCaller()
    {
        var paymentRepository = Substitute.For<IPaymentRepository>();
        var orderId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var ownPayment = new Payment { Id = Guid.NewGuid(), OrderId = orderId, UserId = callerId, Amount = 10m, Currency = "usd", Status = PaymentStatus.Succeeded, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var otherPayment = new Payment { Id = Guid.NewGuid(), OrderId = orderId, UserId = otherUserId, Amount = 10m, Currency = "usd", Status = PaymentStatus.Succeeded, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        paymentRepository.ListByOrderIdAsync(orderId, Arg.Any<CancellationToken>()).Returns([ownPayment, otherPayment]);

        var handler = new ListPaymentsByOrderQueryHandler(paymentRepository);
        var result = await handler.Handle(new ListPaymentsByOrderQuery(orderId, callerId), CancellationToken.None);

        Assert.True(result.Succeeded);
        var single = Assert.Single(result.Value!);
        Assert.Equal(ownPayment.Id, single.Id);
    }

    [Fact]
    public async Task Handle_NoPaymentsForOrder_ReturnsEmptyList()
    {
        var paymentRepository = Substitute.For<IPaymentRepository>();
        var orderId = Guid.NewGuid();
        paymentRepository.ListByOrderIdAsync(orderId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Payment>());

        var handler = new ListPaymentsByOrderQueryHandler(paymentRepository);
        var result = await handler.Handle(new ListPaymentsByOrderQuery(orderId, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Value!);
    }
}
