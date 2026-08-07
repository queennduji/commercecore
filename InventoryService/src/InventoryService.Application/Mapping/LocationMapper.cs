using InventoryService.Application.Dtos;
using InventoryService.Domain.Entities;

namespace InventoryService.Application.Mapping;

public static class LocationMapper
{
    public static LocationDto ToDto(this Location location) => new(
        location.Id,
        location.Name,
        location.Code,
        location.IsActive,
        location.CreatedAt,
        location.UpdatedAt);
}
