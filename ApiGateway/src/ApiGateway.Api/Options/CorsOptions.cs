namespace ApiGateway.Api.Options;

/// <summary>Origins allowed to call the gateway directly from browser JS – needed once
/// Storefront started calling anonymous routes (cart) client-side instead of only server-side.
/// Empty by default in the base appsettings.json; each environment fills in its own dev/prod
/// origins (see appsettings.Development.json for the local Storefront's origin).</summary>
public class CorsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; set; } = [];
}
