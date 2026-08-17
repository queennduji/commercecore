using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ApiGateway.IntegrationTests.Fixtures;

/// <summary>A separate WebApplicationFactory instance (and therefore a separate, isolated
/// rate-limiter state) from <see cref="ApiGatewayFixture"/> — sharing one instance across every
/// test in the assembly would make rate-limit tests order-dependent on however many requests
/// earlier tests happened to send. Configured with a deliberately tiny PermitLimit so a single
/// test can exhaust it without sending hundreds of requests.</summary>
public class RateLimitedApiGatewayFixture : IAsyncLifetime
{
    public const int PermitLimit = 2;

    public FakeBackendServer Backend { get; } = new();
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Backend.StartAsync();

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = ApiGatewayFixture.JwtKey,
                    ["Jwt:Issuer"] = ApiGatewayFixture.JwtIssuer,
                    ["Jwt:Audience"] = ApiGatewayFixture.JwtAudience,
                    ["Otel:ServiceName"] = "ApiGateway.Tests",
                    ["Otel:TracesEndpoint"] = "http://127.0.0.1:1",
                    ["Otel:LogsEndpoint"] = "http://127.0.0.1:1",
                    ["RateLimiting:PermitLimit"] = PermitLimit.ToString(),
                    ["RateLimiting:WindowSeconds"] = "60",
                    ["ReverseProxy:Clusters:catalog-cluster:Destinations:d1:Address"] = Backend.BaseUrl
                });
            });
        });
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await Backend.DisposeAsync();
    }
}
