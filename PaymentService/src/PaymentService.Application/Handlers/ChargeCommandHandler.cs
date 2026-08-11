using PaymentService.Application.Commands;
using PaymentService.Application.Common;
using PaymentService.Application.Dtos;
using PaymentService.Application.Interfaces;
using PaymentService.Application.Mapping;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Events;
using MediatR;

namespace PaymentService.Application.Handlers;

public class ChargeCommandHandler : IRequestHandler<ChargeCommand, ServiceResult<PaymentDto>>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IEventPublisher _eventPublisher;

    public ChargeCommandHandler(
        IPaymentRepository paymentRepository,
        IPaymentGateway paymentGateway,
        IEventPublisher eventPublisher)
    {
        _paymentRepository = paymentRepository;
        _paymentGateway = paymentGateway;
        _eventPublisher = eventPublisher;
    }

    public async Task<ServiceResult<PaymentDto>> Handle(ChargeCommand request, CancellationToken cancellationToken)
    {
        var gatewayResult = await _paymentGateway.ChargeAsync(
            request.Amount,
            request.Currency,
            request.PaymentMethodId,
            $"Order {request.OrderId}",
            cancellationToken);

        var now = DateTime.UtcNow;

        // A Payment row is recorded either way — Succeeded or Failed — so there's always an audit
        // trail of the attempt, not just of successful charges.
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
            UserId = request.UserId,
            Amount = request.Amount,
            Currency = request.Currency,
            Status = gatewayResult.Succeeded ? PaymentStatus.Succeeded : PaymentStatus.Failed,
            ProviderReference = gatewayResult.ProviderReference,
            FailureReason = gatewayResult.FailureReason,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _paymentRepository.AddAsync(payment, cancellationToken);
        await _paymentRepository.SaveChangesAsync(cancellationToken);

        if (!gatewayResult.Succeeded)
        {
            await _eventPublisher.PublishPaymentFailedAsync(new PaymentFailedEvent
            {
                PaymentId = payment.Id,
                OrderId = payment.OrderId,
                UserId = payment.UserId,
                FailureReason = payment.FailureReason ?? "Payment declined.",
                FailedAt = now
            }, cancellationToken);

            return ServiceResult<PaymentDto>.Failure(payment.FailureReason ?? "Payment declined.");
        }

        await _eventPublisher.PublishPaymentSucceededAsync(new PaymentSucceededEvent
        {
            PaymentId = payment.Id,
            OrderId = payment.OrderId,
            UserId = payment.UserId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            SucceededAt = now
        }, cancellationToken);

        return ServiceResult<PaymentDto>.Success(payment.ToDto());
    }
}
