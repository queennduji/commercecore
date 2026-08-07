namespace InventoryService.Application.Dtos;

public record LocationDto(
    Guid Id,
    string Name,
    string Code,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
