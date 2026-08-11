using PaymentService.Application.Interfaces;
using PaymentService.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Stripe;

namespace PaymentService.Infrastructure.Gateway;

/// <summary>
/// Real Stripe test-mode integration via PaymentIntents. paymentMethodId is expected to be one of
/// Stripe's built-in test PaymentMethod ids (e.g. "pm_card_visa" always succeeds,
/// "pm_card_visa_chargeDeclined" always declines) — the officially documented way to exercise the
/// PaymentIntents API server-to-server without a Stripe.js/Elements frontend actually collecting a
/// card. See https://docs.stripe.com/testing.
/// </summary>
public class StripePaymentGateway : IPaymentGateway
{
    private readonly IStripeClient _stripeClient;

    public StripePaymentGateway(IOptions<StripeOptions> options)
    {
        // Constructing a StripeClient does no network I/O itself, so this doesn't need the
        // lazy-factory-delegate pattern used for MinIO/Redis — resolving IOptions<StripeOptions>
        // here, inside the constructor, already happens after the DI container (and any test
        // config overrides) is fully built.
        _stripeClient = new StripeClient(options.Value.SecretKey);
    }

    public async Task<GatewayChargeResult> ChargeAsync(
        decimal amount,
        string currency,
        string paymentMethodId,
        string description,
        CancellationToken cancellationToken = default)
    {
        var service = new PaymentIntentService(_stripeClient);

        try
        {
            var intent = await service.CreateAsync(new PaymentIntentCreateOptions
            {
                Amount = ToSmallestCurrencyUnit(amount),
                Currency = currency.ToLowerInvariant(),
                PaymentMethod = paymentMethodId,
                PaymentMethodTypes = ["card"],
                Confirm = true,
                OffSession = true,
                Description = description
            }, cancellationToken: cancellationToken);

            return intent.Status == "succeeded"
                ? new GatewayChargeResult(true, intent.Id, null)
                : new GatewayChargeResult(false, intent.Id, $"Payment not completed (status: {intent.Status}).");
        }
        catch (StripeException ex)
        {
            // A declined test card (e.g. pm_card_visa_chargeDeclined) surfaces as a StripeException
            // here rather than a non-succeeded PaymentIntent status — this is expected, not a bug.
            return new GatewayChargeResult(false, null, ex.StripeError?.Message ?? ex.Message);
        }
    }

    public async Task<GatewayRefundResult> RefundAsync(string providerReference, CancellationToken cancellationToken = default)
    {
        var service = new RefundService(_stripeClient);

        try
        {
            var refund = await service.CreateAsync(new RefundCreateOptions
            {
                PaymentIntent = providerReference
            }, cancellationToken: cancellationToken);

            return refund.Status is "succeeded" or "pending"
                ? new GatewayRefundResult(true, refund.Id, null)
                : new GatewayRefundResult(false, refund.Id, $"Refund not completed (status: {refund.Status}).");
        }
        catch (StripeException ex)
        {
            return new GatewayRefundResult(false, null, ex.StripeError?.Message ?? ex.Message);
        }
    }

    /// <summary>Stripe amounts are integers in the currency's smallest unit (cents for USD).</summary>
    private static long ToSmallestCurrencyUnit(decimal amount) => (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);
}
