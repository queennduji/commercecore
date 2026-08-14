using ShippingService.Application.Dtos;
using ShippingService.Domain.Entities;

namespace ShippingService.Application.Mapping;

public static class ShipmentMapper
{
    public static ShipmentDto ToDto(this Shipment shipment) => new(
        shipment.Id,
        shipment.OrderId,
        shipment.UserId,
        shipment.ShippingAddress,
        shipment.Status.ToString(),
        shipment.CarrierName,
        shipment.TrackingNumber,
        shipment.ExceptionReason,
        shipment.CreatedAt,
        shipment.UpdatedAt,
        shipment.DispatchedAt,
        shipment.DeliveredAt);
}
