namespace ShippingService.Domain.Events;

/// <summary>Not consumed by OrderService in v1 (a failed/returned shipment doesn't have a
/// corresponding OrderStatus – that stays a fulfillment-side concern for now) – published purely
/// for audit trail and any future ops/notification consumer, mirroring PaymentFailedEvent's role.</summary>
public class ShipmentExceptionEvent
{
    public Guid ShipmentId { get; set; }
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
}
