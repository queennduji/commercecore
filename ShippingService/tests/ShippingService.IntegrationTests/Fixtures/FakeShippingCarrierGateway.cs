using ShippingService.Application.Interfaces;

namespace ShippingService.IntegrationTests.Fixtures;

/// <summary>Stands in for a real EasyPost account – same reasoning as PaymentService's
/// FakePaymentGateway. Defaults to succeeding every create/retrieve call; tests can add a
/// tracking code to <see cref="DeclinedTrackingCodes"/> to force a creation failure, and control
/// what RetrieveTrackerAsync reports back via <see cref="StatusByProviderTrackerId"/>.</summary>
public class FakeShippingCarrierGateway : IShippingCarrierGateway
{
    public HashSet<string> DeclinedTrackingCodes { get; } = [];
    public Dictionary<string, string> StatusByProviderTrackerId { get; } = [];
    public List<(string TrackingCode, string Carrier)> Creates { get; } = [];

    public Task<CarrierTrackerResult> CreateTrackerAsync(string trackingCode, string carrier, CancellationToken cancellationToken = default)
    {
        Creates.Add((trackingCode, carrier));

        if (DeclinedTrackingCodes.Contains(trackingCode))
        {
            return Task.FromResult(new CarrierTrackerResult(false, null, null, "Invalid tracking code."));
        }

        var providerTrackerId = $"trk_fake_{Guid.NewGuid():N}";
        StatusByProviderTrackerId[providerTrackerId] = "pre_transit";
        return Task.FromResult(new CarrierTrackerResult(true, providerTrackerId, "pre_transit", null));
    }

    public Task<CarrierTrackerResult> RetrieveTrackerAsync(string providerTrackerId, CancellationToken cancellationToken = default)
    {
        var status = StatusByProviderTrackerId.GetValueOrDefault(providerTrackerId, "unknown");
        return Task.FromResult(new CarrierTrackerResult(true, providerTrackerId, status, null));
    }
}
