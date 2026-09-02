using OrderService.Application.Commands;
using OrderService.Application.Common;
using OrderService.Application.Dtos;
using OrderService.Application.Interfaces;
using OrderService.Application.Mapping;
using OrderService.Domain.Entities;
using OrderService.Domain.Events;
using MediatR;

namespace OrderService.Application.Handlers;

public class ShipOrderCommandHandler : IRequestHandler<ShipOrderCommand, ServiceResult<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryServiceClient _inventoryServiceClient;
    private readonly IEventPublisher _eventPublisher;

    public ShipOrderCommandHandler(
        IOrderRepository orderRepository,
        IInventoryServiceClient inventoryServiceClient,
        IEventPublisher eventPublisher)
    {
        _orderRepository = orderRepository;
        _inventoryServiceClient = inventoryServiceClient;
        _eventPublisher = eventPublisher;
    }

    public async Task<ServiceResult<OrderDto>> Handle(ShipOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return ServiceResult<OrderDto>.Failure("Order not found.");
        }

        if (order.Status != OrderStatus.Paid)
        {
            return ServiceResult<OrderDto>.Failure($"Cannot ship an order from status {order.Status}.");
        }

        // Commits every line's reservation (stock actually leaves the building). There is no
        // compensating rollback if one commit fails partway through – CommitReservation isn't
        // reversible in InventoryService – so a partial failure here is surfaced as an error and
        // the order stays in Paid, ready to retry once whatever caused the failure is fixed.
        foreach (var item in order.Items)
        {
            var committed = await _inventoryServiceClient.CommitAsync(item.ReservationId, cancellationToken);
            if (!committed)
            {
                return ServiceResult<OrderDto>.Failure($"Failed to commit stock reservation for product {item.ProductId}.");
            }
        }

        var now = DateTime.UtcNow;
        order.Status = OrderStatus.Shipped;
        order.UpdatedAt = now;
        await _orderRepository.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishOrderShippedAsync(new OrderShippedEvent
        {
            OrderId = order.Id,
            UserId = order.UserId,
            ShippedAt = now
        }, cancellationToken);

        return ServiceResult<OrderDto>.Success(order.ToDto());
    }
}
