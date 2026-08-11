using PaymentService.Application.Handlers;
using PaymentService.Application.Interfaces;
using PaymentService.Application.Queries;
using PaymentService.Domain.Entities;
using NSubstitute;

namespace PaymentService.UnitTests.Handlers;

public class GetPaymentQueryHandlerTests
{
    [Fact]
    public async Task Handle_PaymentOwnedByCaller_ReturnsIt()
    {
        var paymentRepository = Substitute.For<IPaymentRepository>();
        var userId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            UserId = userId,
            Amount = 20m,
            Currency = "usd",
            Status = PaymentStatus.Succeeded,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        paymentRepository.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        var handler = new GetPaymentQueryHandler(paymentRepository);
        var result = await handler.Handle(new GetPaymentQuery(payment.Id, userId), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(payment.Id, result.Value!.Id);
    }

    [Fact]
    public async Task Handle_PaymentOwnedByDifferentUser_ReturnsNotFoundStyleFailure()
    {
        var paymentRepository = Substitute.For<IPaymentRepository>();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Amount = 20m,
            Currency = "usd",
            Status = PaymentStatus.Succeeded,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        paymentRepository.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        var handler = new GetPaymentQueryHandler(paymentRepository);
        var result = await handler.Handle(new GetPaymentQuery(payment.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("not found", result.Errors.Single(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_PaymentDoesNotExist_ReturnsFailure()
    {
        var paymentRepository = Substitute.For<IPaymentRepository>();
        paymentRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Payment?)null);

        var handler = new GetPaymentQueryHandler(paymentRepository);
        var result = await handler.Handle(new GetPaymentQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
    }
}
