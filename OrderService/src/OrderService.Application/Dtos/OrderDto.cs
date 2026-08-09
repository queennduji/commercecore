namespace OrderService.Application.Dtos;

public record OrderDto(
    Guid Id,
    Guid UserId,
    string Status,
    string ShippingAddress,
    IReadOnlyList<OrderItemDto> Items,
    decimal Subtotal,
    DateTime CreatedAt,
    DateTime UpdatedAt);
