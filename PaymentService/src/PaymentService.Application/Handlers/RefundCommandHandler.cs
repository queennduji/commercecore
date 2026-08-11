using PaymentService.Application.Commands;
using PaymentService.Application.Common;
using PaymentService.Application.Dtos;
using PaymentService.Application.Interfaces;
using PaymentService.Application.Mapping;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Events;
using MediatR;

namespace PaymentService.Application.Handlers;

public class RefundCommandHandler : IRequestHandler<RefundCommand, ServiceResult<PaymentDto>>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IEventPublisher _eventPublisher;

    public RefundCommandHandler(
        IPaymentRepository paymentRepository,
        IPaymentGateway paymentGateway,
        IEventPublisher eventPublisher)
    {
        _paymentRepository = paymentRepository;
        _paymentGateway = paymentGateway;
        _eventPublisher = eventPublisher;
    }

    public async Task<ServiceResult<PaymentDto>> Handle(RefundCommand request, CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetLatestSucceededByOrderIdAsync(request.OrderId, cancellationToken);
        if (payment is null || payment.ProviderReference is null)
        {
            return ServiceResult<PaymentDto>.Failure("No successful payment found for this order to refund.");
        }

        var gatewayResult = await _paymentGateway.RefundAsync(payment.ProviderReference, cancellationToken);
        if (!gatewayResult.Succeeded)
        {
            return ServiceResult<PaymentDto>.Failure(gatewayResult.FailureReason ?? "Refund failed.");
        }

        var now = DateTime.UtcNow;
        payment.Status = PaymentStatus.Refunded;
        payment.UpdatedAt = now;
        await _paymentRepository.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishPaymentRefundedAsync(new PaymentRefundedEvent
        {
            PaymentId = payment.Id,
            OrderId = payment.OrderId,
            UserId = payment.UserId,
            Amount = payment.Amount,
            RefundedAt = now
        }, cancellationToken);

        return ServiceResult<PaymentDto>.Success(payment.ToDto());
    }
}
