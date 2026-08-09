using OrderService.Application.Commands;
using OrderService.Application.Common;
using OrderService.Application.Dtos;
using OrderService.Application.Interfaces;
using OrderService.Application.Mapping;
using OrderService.Domain.Entities;
using OrderService.Domain.Events;
using MediatR;

namespace OrderService.Application.Handlers;

public class RefundOrderCommandHandler : IRequestHandler<RefundOrderCommand, ServiceResult<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IEventPublisher _eventPublisher;

    public RefundOrderCommandHandler(IOrderRepository orderRepository, IEventPublisher eventPublisher)
    {
        _orderRepository = orderRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<ServiceResult<OrderDto>> Handle(RefundOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return ServiceResult<OrderDto>.Failure("Order not found.");
        }

        if (order.Status is not (OrderStatus.Paid or OrderStatus.Shipped or OrderStatus.Delivered))
        {
            return ServiceResult<OrderDto>.Failure($"Cannot refund an order from status {order.Status}.");
        }

        // Deliberately does not touch inventory: restocking a post-shipment return is out of scope
        // for now (would need its own "return to stock" workflow, not the reserve/commit/release
        // trio InventoryService already exposes).
        var now = DateTime.UtcNow;
        order.Status = OrderStatus.Refunded;
        order.UpdatedAt = now;
        await _orderRepository.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishOrderRefundedAsync(new OrderRefundedEvent
        {
            OrderId = order.Id,
            UserId = order.UserId,
            RefundedAt = now
        }, cancellationToken);

        return ServiceResult<OrderDto>.Success(order.ToDto());
    }
}
