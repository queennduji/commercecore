namespace ShippingService.Infrastructure.Options;

public class EasyPostOptions
{
    public const string SectionName = "EasyPost";

    /// <summary>Your own free EasyPost test-mode API key. Deliberately never committed anywhere in
    /// this repo — see README for how to configure it locally via .NET User Secrets (dev) or an
    /// environment variable (Docker), same credential-hygiene pattern as PaymentService's Stripe
    /// key. This is a real per-account external credential, not an internal trusted-service
    /// secret like the shared JWT signing key.</summary>
    public string ApiKey { get; set; } = string.Empty;
}
