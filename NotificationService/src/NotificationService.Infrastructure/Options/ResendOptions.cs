namespace NotificationService.Infrastructure.Options;

public class ResendOptions
{
    public const string SectionName = "Resend";

    /// <summary>Your own free Resend API key. Deliberately never committed anywhere in this repo —
    /// see README for how to configure it locally via .NET User Secrets (dev) or an environment
    /// variable (Docker), same credential-hygiene pattern as PaymentService's Stripe key and
    /// ShippingService's EasyPost key. This is a real per-account external credential, not an
    /// internal trusted-service secret like the shared JWT signing key.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Resend's zero-domain-verification sandbox sender — works for sending to the
    /// account's own verified email address without configuring a custom domain. See README.</summary>
    public string FromAddress { get; set; } = "CommerceCore <onboarding@resend.dev>";
}
