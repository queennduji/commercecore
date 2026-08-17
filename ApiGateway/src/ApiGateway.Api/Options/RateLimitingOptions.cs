namespace ApiGateway.Api.Options;

public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public int PermitLimit { get; set; } = 100;
    public int WindowSeconds { get; set; } = 60;
}
