namespace OrderService.Domain.Events;

public class OrderShippedEvent
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public DateTime ShippedAt { get; set; }
}
