namespace OrderService.Domain.Events;

public class OrderPaidEvent
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public DateTime PaidAt { get; set; }

    /// <summary>Added for ShippingService, which consumes this event to auto-create a Shipment –
    /// there's no synchronous call back into OrderService for this, so the address rides along
    /// with the event instead.</summary>
    public string ShippingAddress { get; set; } = string.Empty;
}
