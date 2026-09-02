namespace PaymentService.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>Stripe's PaymentIntent id (charge) or Refund id (once refunded) – the id you'd
    /// look this transaction up by in the Stripe dashboard.</summary>
    public string? ProviderReference { get; set; }

    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
