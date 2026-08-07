using InventoryService.Application.Dtos;
using InventoryService.Domain.Entities;

namespace InventoryService.Application.Mapping;

public static class StockReservationMapper
{
    public static StockReservationDto ToDto(this StockReservation reservation) => new(
        reservation.Id,
        reservation.ProductId,
        reservation.LocationId,
        reservation.Quantity,
        reservation.Status.ToString(),
        reservation.ReferenceId,
        reservation.CreatedAt,
        reservation.UpdatedAt);
}
