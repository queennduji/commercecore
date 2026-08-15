namespace NotificationService.Infrastructure.Options;

public class TwilioOptions
{
    public const string SectionName = "Twilio";

    /// <summary>Your own Twilio Account SID and Auth Token. Deliberately never committed anywhere
    /// in this repo — see README for how to configure them locally via .NET User Secrets (dev) or
    /// environment variables (Docker), same credential-hygiene pattern as PaymentService's Stripe
    /// key / ShippingService's EasyPost key / this service's own Resend key. Real per-account
    /// external credentials, not internal trusted-service secrets like the shared JWT signing key.</summary>
    public string AccountSid { get; set; } = string.Empty;

    public string AuthToken { get; set; } = string.Empty;

    /// <summary>The Twilio phone number (E.164) messages are sent from — trial accounts get one
    /// free with the account. See README.</summary>
    public string FromPhoneNumber { get; set; } = string.Empty;
}
