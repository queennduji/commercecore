using PaymentService.Application.Interfaces;

namespace PaymentService.IntegrationTests.Fixtures;

/// <summary>Stands in for Stripe — no real Stripe account is needed to run this test suite.
/// Defaults to succeeding every charge/refund; tests can add a paymentMethodId to
/// <see cref="DeclinedPaymentMethodIds"/> or a providerReference to
/// <see cref="RefundFailureProviderReferences"/> to force a decline/refund failure and assert the
/// handler responds correctly.</summary>
public class FakePaymentGateway : IPaymentGateway
{
    public HashSet<string> DeclinedPaymentMethodIds { get; } = [];
    public HashSet<string> RefundFailureProviderReferences { get; } = [];
    public List<(decimal Amount, string Currency, string PaymentMethodId)> Charges { get; } = [];
    public List<string> Refunds { get; } = [];

    public Task<GatewayChargeResult> ChargeAsync(decimal amount, string currency, string paymentMethodId, string description, CancellationToken cancellationToken = default)
    {
        Charges.Add((amount, currency, paymentMethodId));

        if (DeclinedPaymentMethodIds.Contains(paymentMethodId))
        {
            return Task.FromResult(new GatewayChargeResult(false, null, "Your card was declined."));
        }

        return Task.FromResult(new GatewayChargeResult(true, $"pi_fake_{Guid.NewGuid():N}", null));
    }

    public Task<GatewayRefundResult> RefundAsync(string providerReference, CancellationToken cancellationToken = default)
    {
        Refunds.Add(providerReference);

        if (RefundFailureProviderReferences.Contains(providerReference))
        {
            return Task.FromResult(new GatewayRefundResult(false, null, "Charge already refunded."));
        }

        return Task.FromResult(new GatewayRefundResult(true, $"re_fake_{Guid.NewGuid():N}", null));
    }
}
