namespace PaymentService.Application.Interfaces;

public record GatewayChargeResult(bool Succeeded, string? ProviderReference, string? FailureReason);

public record GatewayRefundResult(bool Succeeded, string? ProviderReference, string? FailureReason);

/// <summary>Abstraction over the real payment processor (Stripe) so the Application layer never
/// depends on Stripe.net directly, and so integration tests can swap in a deterministic fake
/// without needing a real Stripe account.</summary>
public interface IPaymentGateway
{
    Task<GatewayChargeResult> ChargeAsync(
        decimal amount,
        string currency,
        string paymentMethodId,
        string description,
        CancellationToken cancellationToken = default);

    Task<GatewayRefundResult> RefundAsync(string providerReference, CancellationToken cancellationToken = default);
}
