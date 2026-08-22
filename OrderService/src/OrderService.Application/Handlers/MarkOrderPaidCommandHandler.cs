using OrderService.Application.Commands;
using OrderService.Application.Common;
using OrderService.Application.Dtos;
using OrderService.Application.Interfaces;
using OrderService.Application.Mapping;
using OrderService.Domain.Entities;
using OrderService.Domain.Events;
using MediatR;

namespace OrderService.Application.Handlers;

public class MarkOrderPaidCommandHandler : IRequestHandler<MarkOrderPaidCommand, ServiceResult<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentServiceClient _paymentServiceClient;
    private readonly IEventPublisher _eventPublisher;
    private readonly IOrderPaymentLock _orderPaymentLock;

    public MarkOrderPaidCommandHandler(
        IOrderRepository orderRepository,
        IPaymentServiceClient paymentServiceClient,
        IEventPublisher eventPublisher,
        IOrderPaymentLock orderPaymentLock)
    {
        _orderRepository = orderRepository;
        _paymentServiceClient = paymentServiceClient;
        _eventPublisher = eventPublisher;
        _orderPaymentLock = orderPaymentLock;
    }

    public async Task<ServiceResult<OrderDto>> Handle(MarkOrderPaidCommand request, CancellationToken cancellationToken)
    {
        // Serializes every /pay attempt for this order - across all OrderService instances, not
        // just this process - so a concurrent duplicate request waits here instead of racing the
        // status check below. Without this, two concurrent requests could both read Pending before
        // either committed Paid, and both would go on to call PaymentService concurrently -
        // PaymentService's own lock/idempotency-key/constraint chain already makes that safe
        // against an actual double-charge, but this closes the race at its source instead of
        // relying solely on the downstream service to absorb it.
        await using var _ = await _orderPaymentLock.AcquireAsync(request.OrderId, cancellationToken);

        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null || order.UserId != request.UserId)
        {
            return ServiceResult<OrderDto>.Failure("Order not found.");
        }

        // Already reached the exact state this command is trying to reach - most likely a
        // duplicate/retried request that waited on the lock above while an earlier one finished.
        // Treat as success (idempotent), not an error: the order genuinely is paid, which is what
        // the caller asked for.
        if (order.Status == OrderStatus.Paid)
        {
            return ServiceResult<OrderDto>.Success(order.ToDto());
        }

        if (order.Status != OrderStatus.Pending)
        {
            return ServiceResult<OrderDto>.Failure($"Cannot mark an order as paid from status {order.Status}.");
        }

        var subtotal = order.Items.Sum(i => i.UnitPrice * i.Quantity);

        // The platform has no multi-currency concept anywhere else (Catalog prices carry no
        // currency), so "usd" is hardcoded here too rather than threading a currency field through
        // every upstream service just for this.
        var paymentResult = await _paymentServiceClient.ChargeAsync(order.Id, subtotal, "usd", request.PaymentMethodId, cancellationToken);
        if (!paymentResult.Succeeded)
        {
            return ServiceResult<OrderDto>.Failure(paymentResult.FailureReason ?? "Payment failed.");
        }

        var now = DateTime.UtcNow;
        order.Status = OrderStatus.Paid;
        order.UpdatedAt = now;
        await _orderRepository.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishOrderPaidAsync(new OrderPaidEvent
        {
            OrderId = order.Id,
            UserId = order.UserId,
            PaidAt = now,
            ShippingAddress = order.ShippingAddress
        }, cancellationToken);

        return ServiceResult<OrderDto>.Success(order.ToDto());
    }
}
