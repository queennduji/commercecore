namespace OrderService.Domain.Events;

public class OrderPaidEvent
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public DateTime PaidAt { get; set; }
}
