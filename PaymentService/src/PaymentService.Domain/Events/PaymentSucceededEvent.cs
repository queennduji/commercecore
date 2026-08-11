namespace PaymentService.Domain.Events;

public class PaymentSucceededEvent
{
    public Guid PaymentId { get; set; }
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime SucceededAt { get; set; }
}
