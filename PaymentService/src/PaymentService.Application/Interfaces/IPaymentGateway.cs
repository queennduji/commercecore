namespace PaymentService.Application.Interfaces;

public record GatewayChargeResult(bool Succeeded, string? ProviderReference, string? FailureReason);

public record GatewayRefundResult(bool Succeeded, string? ProviderReference, string? FailureReason);

/// <summary>Abstraction over the real payment processor (Stripe) so the Application layer never
/// depends on Stripe.net directly, and so integration tests can swap in a deterministic fake
/// without needing a real Stripe account.</summary>
public interface IPaymentGateway
{
    /// <summary>
    /// <paramref name="idempotencyKey"/> must stay the same across retries of the *same* logical
    /// charge (e.g. one value per OrderId, not a new value per attempt) - Stripe uses it to
    /// recognize a retried request as one it already processed and return the original result
    /// instead of creating a second charge. Required now that the HttpClient underneath this call
    /// has automatic retry/circuit-breaker resilience (see DependencyInjection's "Stripe" named
    /// client) - without it, a retry after a dropped-but-actually-successful response would double
    /// charge the customer.
    /// </summary>
    Task<GatewayChargeResult> ChargeAsync(
        decimal amount,
        string currency,
        string paymentMethodId,
        string description,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<GatewayRefundResult> RefundAsync(string providerReference, CancellationToken cancellationToken = default);
}
