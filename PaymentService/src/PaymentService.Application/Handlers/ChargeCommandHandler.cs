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
    private readonly IOrderChargeLock _orderChargeLock;

    public ChargeCommandHandler(
        IPaymentRepository paymentRepository,
        IPaymentGateway paymentGateway,
        IEventPublisher eventPublisher,
        IOrderChargeLock orderChargeLock)
    {
        _paymentRepository = paymentRepository;
        _paymentGateway = paymentGateway;
        _eventPublisher = eventPublisher;
        _orderChargeLock = orderChargeLock;
    }

    public async Task<ServiceResult<PaymentDto>> Handle(ChargeCommand request, CancellationToken cancellationToken)
    {
        // Serializes every charge attempt for this order - across all PaymentService instances,
        // not just this process - so a concurrent duplicate request waits here instead of racing
        // this handler's check-then-act below and potentially reaching Stripe at the same time as
        // the original. The DB-level unique constraint further down still exists as a backstop
        // (see PaymentRepository.SaveChangesAsync) for if this lock is ever bypassed or misbehaves
        // - it's what guarantees correctness; the lock is what avoids the wasted duplicate work.
        await using var _ = await _orderChargeLock.AcquireAsync(request.OrderId, cancellationToken);

        var existingPayment = await _paymentRepository.GetLatestSucceededByOrderIdAsync(request.OrderId, cancellationToken);
        if (existingPayment is not null)
        {
            return ServiceResult<PaymentDto>.Success(existingPayment.ToDto());
        }

        // OrderId, not a freshly generated value - this must stay the same across every retry of
        // this same logical charge (including ones Polly's resilience handler does automatically
        // underneath ChargeAsync) so Stripe treats them as one request. An order is only ever
        // charged once in this flow, so OrderId is already the right uniqueness boundary.
        var gatewayResult = await _paymentGateway.ChargeAsync(
            request.Amount,
            request.Currency,
            request.PaymentMethodId,
            $"Order {request.OrderId}",
            request.OrderId.ToString(),
            cancellationToken);

        var now = DateTime.UtcNow;

        // A Payment row is recorded either way – Succeeded or Failed – so there's always an audit
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

        try
        {
            await _paymentRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateSucceededPaymentException)
        {
            // Lost the race: another request for this order committed its own Succeeded payment
            // between this handler's check above and this insert. The charge itself already
            // happened against Stripe under this same OrderId-derived idempotency key, so it's the
            // same money either way - converge on whichever payment actually won rather than
            // surfacing a 500 for what is, from the caller's point of view, a successful charge.
            var winner = await _paymentRepository.GetLatestSucceededByOrderIdAsync(request.OrderId, cancellationToken);
            return ServiceResult<PaymentDto>.Success(winner!.ToDto());
        }

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
