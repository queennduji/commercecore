using CartService.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.Redis;

namespace CartService.IntegrationTests.Fixtures;

public class CartApiFixture : IAsyncLifetime
{
    public static readonly string JwtKey = Convert.ToBase64String(new byte[32]);
    public const string JwtIssuer = "CommerceCore.AuthenticationService.Tests";
    public const string JwtAudience = "CommerceCore.Tests";

    private RedisContainer _redis = null!;

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public FakeCatalogServiceClient CatalogServiceClient { get; } = new();

    public async Task InitializeAsync()
    {
        _redis = new RedisBuilder("redis:8.8.0").Build();
        await _redis.StartAsync();

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Redis:ConnectionString"] = _redis.GetConnectionString(),
                    ["Redis:TtlDays"] = "30",
                    ["Jwt:Key"] = JwtKey,
                    ["Jwt:Issuer"] = JwtIssuer,
                    ["Jwt:Audience"] = JwtAudience,
                    ["CatalogService:BaseUrl"] = "http://catalog-service.invalid",
                    // Never actually reached in tests – OTLP export failures are non-fatal at
                    // runtime, so a real collector isn't needed here. Only present because Otel
                    // config is required at startup, same as Jwt above.
                    ["Otel:ServiceName"] = "CartService.Tests",
                    ["Otel:TracesEndpoint"] = "http://127.0.0.1:1",
                    ["Otel:LogsEndpoint"] = "http://127.0.0.1:1"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ICatalogServiceClient>();
                services.AddSingleton<ICatalogServiceClient>(CatalogServiceClient);
            });
        });
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _redis.DisposeAsync();
    }
}
