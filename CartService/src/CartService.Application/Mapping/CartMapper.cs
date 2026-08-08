using CartService.Application.Dtos;
using CartService.Domain.Entities;

namespace CartService.Application.Mapping;

public static class CartMapper
{
    public static CartDto ToDto(this Cart cart)
    {
        var items = cart.Items
            .Select(i => new CartItemDto(i.ProductId, i.Sku, i.Name, i.UnitPrice, i.Quantity, i.UnitPrice * i.Quantity))
            .ToList();

        return new CartDto(
            cart.Id,
            cart.UserId,
            items,
            items.Sum(i => i.LineTotal),
            cart.CreatedAt,
            cart.UpdatedAt);
    }
}
