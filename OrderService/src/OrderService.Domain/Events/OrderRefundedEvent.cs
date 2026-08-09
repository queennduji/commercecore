namespace OrderService.Domain.Events;

public class OrderRefundedEvent
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public DateTime RefundedAt { get; set; }
}
