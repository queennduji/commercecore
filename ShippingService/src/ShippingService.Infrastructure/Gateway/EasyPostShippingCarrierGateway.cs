using ShippingService.Application.Interfaces;
using ShippingService.Infrastructure.Options;
using EasyPost;
using EasyPost.Exceptions;
using Microsoft.Extensions.Options;

namespace ShippingService.Infrastructure.Gateway;

/// <summary>Real EasyPost integration, test mode. Deliberately never purchases a real shipping
/// label or address-validates — this platform doesn't model parcel weight/dimensions or
/// structured addresses, so the only real API surface exercised is the Tracker resource: creating
/// a tracker from a tracking code (EasyPost's own test codes like EZ2000000002 simulate real
/// carrier tracking behavior end to end) and polling it for status. Mirrors StripePaymentGateway's
/// role in PaymentService — the Application layer never depends on the EasyPost SDK directly.</summary>
public class EasyPostShippingCarrierGateway : IShippingCarrierGateway
{
    private readonly Client _client;

    public EasyPostShippingCarrierGateway(IOptions<EasyPostOptions> options, IHttpClientFactory httpClientFactory)
    {
        // The "EasyPost" named client (see DependencyInjection) carries Polly's standard
        // resilience handler - passing it via CustomHttpClient is what puts these calls through
        // that pipeline instead of whatever default HttpClient the SDK would otherwise create.
        _client = new Client(new ClientConfiguration(options.Value.ApiKey)
        {
            CustomHttpClient = httpClientFactory.CreateClient("EasyPost")
        });
    }

    public async Task<CarrierTrackerResult> CreateTrackerAsync(string trackingCode, string carrier, CancellationToken cancellationToken = default)
    {
        try
        {
            // EasyPost.Services.TrackerService.Create's positional overload is (carrier,
            // trackingCode, ct) - the reverse of this method's own (trackingCode, carrier)
            // parameter order. Passing them straight through here previously sent our tracking
            // code as EasyPost's "carrier" and our carrier name as its "tracking code", which
            // EasyPost's test-mode validation caught (it rejected "USPS" for not being one of
            // the seven canned test tracking numbers) - confirmed live against the real API.
            var tracker = await _client.Tracker.Create(carrier, trackingCode, cancellationToken);
            return new CarrierTrackerResult(true, tracker.Id, tracker.Status, null);
        }
        catch (EasyPostError ex)
        {
            return new CarrierTrackerResult(false, null, null, ex.Message);
        }
    }

    public async Task<CarrierTrackerResult> RetrieveTrackerAsync(string providerTrackerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var tracker = await _client.Tracker.Retrieve(providerTrackerId, cancellationToken);
            return new CarrierTrackerResult(true, tracker.Id, tracker.Status, null);
        }
        catch (EasyPostError ex)
        {
            return new CarrierTrackerResult(false, null, null, ex.Message);
        }
    }
}
