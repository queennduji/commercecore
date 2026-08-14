namespace ShippingService.Application.Dtos;

public record ShipmentDto(
    Guid Id,
    Guid OrderId,
    Guid UserId,
    string ShippingAddress,
    string Status,
    string? CarrierName,
    string? TrackingNumber,
    string? ExceptionReason,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? DispatchedAt,
    DateTime? DeliveredAt);
