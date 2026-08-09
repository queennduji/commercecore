using OrderService.Application.Commands;
using OrderService.Application.Common;
using OrderService.Application.Dtos;
using OrderService.Application.Interfaces;
using OrderService.Application.Mapping;
using OrderService.Domain.Entities;
using OrderService.Domain.Events;
using MediatR;

namespace OrderService.Application.Handlers;

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, ServiceResult<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryServiceClient _inventoryServiceClient;
    private readonly IEventPublisher _eventPublisher;

    public CancelOrderCommandHandler(
        IOrderRepository orderRepository,
        IInventoryServiceClient inventoryServiceClient,
        IEventPublisher eventPublisher)
    {
        _orderRepository = orderRepository;
        _inventoryServiceClient = inventoryServiceClient;
        _eventPublisher = eventPublisher;
    }

    public async Task<ServiceResult<OrderDto>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null || order.UserId != request.UserId)
        {
            return ServiceResult<OrderDto>.Failure("Order not found.");
        }

        if (order.Status is not (OrderStatus.Pending or OrderStatus.Paid))
        {
            return ServiceResult<OrderDto>.Failure($"Cannot cancel an order from status {order.Status}.");
        }

        foreach (var item in order.Items)
        {
            await _inventoryServiceClient.ReleaseAsync(item.ReservationId, cancellationToken);
        }

        var now = DateTime.UtcNow;
        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = now;
        await _orderRepository.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishOrderCancelledAsync(new OrderCancelledEvent
        {
            OrderId = order.Id,
            UserId = order.UserId,
            CancelledAt = now
        }, cancellationToken);

        return ServiceResult<OrderDto>.Success(order.ToDto());
    }
}
