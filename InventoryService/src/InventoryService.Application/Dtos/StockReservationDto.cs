namespace InventoryService.Application.Dtos;

public record StockReservationDto(
    Guid Id,
    Guid ProductId,
    Guid LocationId,
    int Quantity,
    string Status,
    string? ReferenceId,
    DateTime CreatedAt,
    DateTime UpdatedAt);
