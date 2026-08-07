using InventoryService.Application.Dtos;
using InventoryService.Domain.Entities;

namespace InventoryService.Application.Mapping;

public static class InventoryItemMapper
{
    public static InventoryItemDto ToDto(this InventoryItem item) => new(
        item.Id,
        item.ProductId,
        item.LocationId,
        item.OnHand,
        item.Reserved,
        item.Available,
        item.CreatedAt,
        item.UpdatedAt);
}
