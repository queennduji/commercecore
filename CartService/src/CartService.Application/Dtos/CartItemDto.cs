namespace CartService.Application.Dtos;

public record CartItemDto(
    Guid ProductId,
    string Sku,
    string Name,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);
