using OrderService.Application.Commands;
using OrderService.Application.Common;
using OrderService.Application.Dtos;
using OrderService.Application.Interfaces;
using OrderService.Application.Mapping;
using OrderService.Domain.Entities;
using OrderService.Domain.Events;
using MediatR;

namespace OrderService.Application.Handlers;

public class CheckoutCommandHandler : IRequestHandler<CheckoutCommand, ServiceResult<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICartServiceClient _cartServiceClient;
    private readonly IInventoryServiceClient _inventoryServiceClient;
    private readonly IEventPublisher _eventPublisher;

    public CheckoutCommandHandler(
        IOrderRepository orderRepository,
        ICartServiceClient cartServiceClient,
        IInventoryServiceClient inventoryServiceClient,
        IEventPublisher eventPublisher)
    {
        _orderRepository = orderRepository;
        _cartServiceClient = cartServiceClient;
        _inventoryServiceClient = inventoryServiceClient;
        _eventPublisher = eventPublisher;
    }

    public async Task<ServiceResult<OrderDto>> Handle(CheckoutCommand request, CancellationToken cancellationToken)
    {
        var cart = await _cartServiceClient.GetCartAsync(request.UserId, cancellationToken);
        if (cart is null || cart.Items.Count == 0)
        {
            return ServiceResult<OrderDto>.Failure("Cart is empty.");
        }

        var orderId = Guid.NewGuid();
        var reservedItems = new List<OrderItem>();

        foreach (var line in cart.Items)
        {
            var stockByLocation = await _inventoryServiceClient.GetStockAsync(line.ProductId, cancellationToken);
            var location = stockByLocation.FirstOrDefault(l => l.Available >= line.Quantity);

            if (location is null)
            {
                await ReleaseAllAsync(reservedItems, cancellationToken);
                return ServiceResult<OrderDto>.Failure($"Insufficient stock for product {line.ProductId}.");
            }

            var reservationId = await _inventoryServiceClient.ReserveAsync(
                line.ProductId, location.LocationId, line.Quantity, orderId.ToString(), cancellationToken);

            if (reservationId is null)
            {
                await ReleaseAllAsync(reservedItems, cancellationToken);
                return ServiceResult<OrderDto>.Failure($"Failed to reserve stock for product {line.ProductId}.");
            }

            reservedItems.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                ProductId = line.ProductId,
                Sku = line.Sku,
                Name = line.Name,
                UnitPrice = line.UnitPrice,
                Quantity = line.Quantity,
                LocationId = location.LocationId,
                ReservationId = reservationId.Value
            });
        }

        var now = DateTime.UtcNow;
        var order = new Order
        {
            Id = orderId,
            UserId = request.UserId,
            Status = OrderStatus.Pending,
            ShippingAddress = request.ShippingAddress,
            Items = reservedItems,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _orderRepository.AddAsync(order, cancellationToken);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        await _cartServiceClient.ClearCartAsync(request.UserId, cancellationToken);

        var dto = order.ToDto();

        await _eventPublisher.PublishOrderCreatedAsync(new OrderCreatedEvent
        {
            OrderId = order.Id,
            UserId = order.UserId,
            Subtotal = dto.Subtotal,
            CreatedAt = now
        }, cancellationToken);

        return ServiceResult<OrderDto>.Success(dto);
    }

    /// <summary>Compensating action for a partially-reserved checkout attempt: gives back every
    /// reservation already made before the failure was hit, so a failed checkout never leaves
    /// stray holds on stock.</summary>
    private async Task ReleaseAllAsync(IEnumerable<OrderItem> reservedItems, CancellationToken cancellationToken)
    {
        foreach (var item in reservedItems)
        {
            await _inventoryServiceClient.ReleaseAsync(item.ReservationId, cancellationToken);
        }
    }
}
