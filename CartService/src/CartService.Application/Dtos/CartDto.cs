namespace CartService.Application.Dtos;

public record CartDto(
    Guid Id,
    Guid? UserId,
    IReadOnlyList<CartItemDto> Items,
    decimal Subtotal,
    DateTime CreatedAt,
    DateTime UpdatedAt);
