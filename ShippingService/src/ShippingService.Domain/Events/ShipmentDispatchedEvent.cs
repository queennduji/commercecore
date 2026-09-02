namespace ShippingService.Domain.Events;

/// <summary>Consumed by OrderService to flip Order.Status Paid -> Shipped – this is the event that
/// replaced the old manual "POST /api/orders/{id}/ship" ops action.</summary>
public class ShipmentDispatchedEvent
{
    public Guid ShipmentId { get; set; }
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public string CarrierName { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
    public DateTime DispatchedAt { get; set; }
}
