using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ApiGateway.IntegrationTests.Fixtures;

/// <summary>Hosts the real gateway (real YARP pipeline, real JWT auth middleware) with every
/// cluster's destination overridden to point at a single <see cref="FakeBackendServer"/> – that
/// server standing in for whichever backend service a given test's route would otherwise reach.
/// Proving which physical service each path prefix reaches is the live Docker smoke test's job
/// (real, distinct services on their real ports); this fixture's job is proving the gateway's own
/// logic – the per-route AuthorizationPolicy enforcement – behaves correctly.</summary>
public class ApiGatewayFixture : IAsyncLifetime
{
    public static readonly string JwtKey = Convert.ToBase64String(new byte[32]);
    public const string JwtIssuer = "CommerceCore.AuthenticationService.Tests";
    public const string JwtAudience = "CommerceCore.Tests";

    private static readonly string[] ClusterIds =
    [
        "auth-cluster", "catalog-cluster", "inventory-cluster", "cart-cluster",
        "order-cluster", "payment-cluster", "shipping-cluster", "notification-cluster"
    ];

    public FakeBackendServer Backend { get; } = new();
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Backend.StartAsync();

        var overrides = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = JwtKey,
            ["Jwt:Issuer"] = JwtIssuer,
            ["Jwt:Audience"] = JwtAudience,
            // Never actually reached in tests – OTLP export failures are non-fatal at runtime
            // (logged and dropped, not thrown), so a real collector isn't needed here. Only
            // present because Otel config is required at startup, same as Jwt above.
            ["Otel:ServiceName"] = "ApiGateway.Tests",
            ["Otel:TracesEndpoint"] = "http://127.0.0.1:1",
            ["Otel:LogsEndpoint"] = "http://127.0.0.1:1"
        };
        foreach (var clusterId in ClusterIds)
        {
            overrides[$"ReverseProxy:Clusters:{clusterId}:Destinations:d1:Address"] = Backend.BaseUrl;
        }

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(overrides);
            });
        });
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await Backend.DisposeAsync();
    }
}
