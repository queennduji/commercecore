using OrderService.Application.Dtos;
using OrderService.Domain.Entities;

namespace OrderService.Application.Mapping;

public static class OrderMapper
{
    public static OrderDto ToDto(this Order order)
    {
        var items = order.Items
            .Select(i => new OrderItemDto(i.ProductId, i.Sku, i.Name, i.UnitPrice, i.Quantity, i.UnitPrice * i.Quantity, i.LocationId))
            .ToList();

        return new OrderDto(
            order.Id,
            order.UserId,
            order.Status.ToString(),
            order.ShippingAddress,
            items,
            items.Sum(i => i.LineTotal),
            order.CreatedAt,
            order.UpdatedAt);
    }
}
