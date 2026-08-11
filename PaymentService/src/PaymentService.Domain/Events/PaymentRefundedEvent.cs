namespace PaymentService.Domain.Events;

public class PaymentRefundedEvent
{
    public Guid PaymentId { get; set; }
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public DateTime RefundedAt { get; set; }
}
