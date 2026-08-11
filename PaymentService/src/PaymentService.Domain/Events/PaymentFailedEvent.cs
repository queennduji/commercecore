namespace PaymentService.Domain.Events;

public class PaymentFailedEvent
{
    public Guid PaymentId { get; set; }
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public string FailureReason { get; set; } = string.Empty;
    public DateTime FailedAt { get; set; }
}
