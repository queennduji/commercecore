namespace PaymentService.Infrastructure.Options;

public class StripeOptions
{
    public const string SectionName = "Stripe";

    /// <summary>Your own Stripe test-mode secret key (starts with sk_test_). Deliberately never
    /// committed anywhere in this repo – see README for how to configure it locally via .NET User
    /// Secrets (dev) or an environment variable (Docker). Unlike the shared JWT signing key, this
    /// is a real per-account external credential, not an internal trusted-service secret.</summary>
    public string SecretKey { get; set; } = string.Empty;
}
