namespace InventoryService.Application.Dtos;

public record InventoryItemDto(
    Guid Id,
    Guid ProductId,
    Guid LocationId,
    int OnHand,
    int Reserved,
    int Available,
    DateTime CreatedAt,
    DateTime UpdatedAt);
