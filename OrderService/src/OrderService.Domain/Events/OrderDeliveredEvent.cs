namespace OrderService.Domain.Events;

public class OrderDeliveredEvent
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public DateTime DeliveredAt { get; set; }
}
