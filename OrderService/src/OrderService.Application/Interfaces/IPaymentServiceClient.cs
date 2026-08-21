namespace OrderService.Application.Interfaces;

public record PaymentResult(bool Succeeded, string? FailureReason);

/// <summary>Synchronous HTTP call to PaymentService, forwarding the caller's own JWT (via
/// ForwardAuthorizationHandler) since PaymentService's charge/refund endpoints require
/// [Authorize]. Keeps the orchestrated-saga design already used for Cart/Inventory — Order
/// explicitly calls out and waits for a real result rather than firing an event and hoping.</summary>
public interface IPaymentServiceClient
{
    Task<PaymentResult> ChargeAsync(Guid orderId, decimal amount, string currency, string paymentMethodId, CancellationToken cancellationToken = default);

    Task<PaymentResult> RefundAsync(Guid orderId, CancellationToken cancellationToken = default);
}
