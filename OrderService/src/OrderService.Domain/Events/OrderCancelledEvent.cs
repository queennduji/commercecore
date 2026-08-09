namespace OrderService.Domain.Events;

public class OrderCancelledEvent
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CancelledAt { get; set; }
}
