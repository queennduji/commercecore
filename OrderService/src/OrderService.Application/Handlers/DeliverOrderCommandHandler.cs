using OrderService.Application.Commands;
using OrderService.Application.Common;
using OrderService.Application.Dtos;
using OrderService.Application.Interfaces;
using OrderService.Application.Mapping;
using OrderService.Domain.Entities;
using OrderService.Domain.Events;
using MediatR;

namespace OrderService.Application.Handlers;

public class DeliverOrderCommandHandler : IRequestHandler<DeliverOrderCommand, ServiceResult<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IEventPublisher _eventPublisher;

    public DeliverOrderCommandHandler(IOrderRepository orderRepository, IEventPublisher eventPublisher)
    {
        _orderRepository = orderRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<ServiceResult<OrderDto>> Handle(DeliverOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return ServiceResult<OrderDto>.Failure("Order not found.");
        }

        if (order.Status != OrderStatus.Shipped)
        {
            return ServiceResult<OrderDto>.Failure($"Cannot deliver an order from status {order.Status}.");
        }

        var now = DateTime.UtcNow;
        order.Status = OrderStatus.Delivered;
        order.UpdatedAt = now;
        await _orderRepository.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishOrderDeliveredAsync(new OrderDeliveredEvent
        {
            OrderId = order.Id,
            UserId = order.UserId,
            DeliveredAt = now
        }, cancellationToken);

        return ServiceResult<OrderDto>.Success(order.ToDto());
    }
}
