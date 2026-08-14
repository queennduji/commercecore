namespace ShippingService.Application.Interfaces;

public record CarrierTrackerResult(bool Succeeded, string? ProviderTrackerId, string? CarrierStatus, string? FailureReason);

/// <summary>Abstraction over the real carrier tracking processor (EasyPost) so the Application
/// layer never depends on the EasyPost SDK directly, and so integration tests can swap in a
/// deterministic fake without needing a real EasyPost account. Mirrors PaymentService's
/// IPaymentGateway split.
///
/// This service never purchases a real shipping label or address-validates — it only creates and
/// polls EasyPost Trackers, using EasyPost's own test tracking codes (EZ1000000001 etc, see
/// EasyPostShippingCarrierGateway) to exercise the real API without needing structured
/// addresses/parcel dimensions this platform doesn't otherwise model.</summary>
public interface IShippingCarrierGateway
{
    Task<CarrierTrackerResult> CreateTrackerAsync(string trackingCode, string carrier, CancellationToken cancellationToken = default);

    Task<CarrierTrackerResult> RetrieveTrackerAsync(string providerTrackerId, CancellationToken cancellationToken = default);
}
