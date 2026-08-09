namespace OrderService.Application.Dtos;

public record OrderItemDto(
    Guid ProductId,
    string Sku,
    string Name,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    Guid LocationId);
