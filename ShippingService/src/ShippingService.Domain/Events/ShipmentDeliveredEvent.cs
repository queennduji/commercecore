namespace ShippingService.Domain.Events;

/// <summary>Consumed by OrderService to flip Order.Status Shipped -> Delivered — replaced the old
/// manual "POST /api/orders/{id}/deliver" ops action.</summary>
public class ShipmentDeliveredEvent
{
    public Guid ShipmentId { get; set; }
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public DateTime DeliveredAt { get; set; }
}
