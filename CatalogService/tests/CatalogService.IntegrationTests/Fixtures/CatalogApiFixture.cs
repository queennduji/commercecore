using CatalogService.Infrastructure.Persistence;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Kafka;
using Testcontainers.Minio;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using IContainer = DotNet.Testcontainers.Containers.IContainer;

namespace CatalogService.IntegrationTests.Fixtures;

public class CatalogApiFixture : IAsyncLifetime
{
    private const string KafkaNetworkAlias = "kafka";
    private const string KafkaInternalListener = KafkaNetworkAlias + ":19092";
    private const int SchemaRegistryPort = 8081;

    public static readonly string JwtKey = Convert.ToBase64String(new byte[32]);
    public const string JwtIssuer = "CommerceCore.AuthenticationService.Tests";
    public const string JwtAudience = "CommerceCore.Tests";

    private INetwork _network = null!;
    private PostgreSqlContainer _postgres = null!;
    private KafkaContainer _kafka = null!;
    private IContainer _schemaRegistry = null!;
    private MinioContainer _minio = null!;
    private RedisContainer _redis = null!;

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public string MinioPublicEndpoint { get; private set; } = string.Empty;
    public string MinioAccessKey { get; private set; } = string.Empty;
    public string MinioSecretKey { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _network = new NetworkBuilder().Build();
        await _network.CreateAsync();

        _postgres = new PostgreSqlBuilder("postgres:16")
            .WithNetwork(_network)
            .WithNetworkAliases("postgres")
            .WithDatabase("catalog_service_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        _kafka = new KafkaBuilder("confluentinc/cp-kafka:7.7.1")
            .WithNetwork(_network)
            .WithNetworkAliases(KafkaNetworkAlias)
            .WithListener(KafkaInternalListener)
            .Build();

        await Task.WhenAll(_postgres.StartAsync(), _kafka.StartAsync());

        _schemaRegistry = new ContainerBuilder("confluentinc/cp-schema-registry:7.7.1")
            .WithNetwork(_network)
            .WithNetworkAliases("schema-registry")
            .WithPortBinding(SchemaRegistryPort, true)
            .WithEnvironment("SCHEMA_REGISTRY_HOST_NAME", "schema-registry")
            .WithEnvironment("SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS", $"PLAINTEXT://{KafkaInternalListener}")
            .WithEnvironment("SCHEMA_REGISTRY_LISTENERS", $"http://0.0.0.0:{SchemaRegistryPort}")
            .Build();

        await _schemaRegistry.StartAsync();
        await WaitForSchemaRegistryAsync();

        _minio = new MinioBuilder("minio/minio:RELEASE.2025-09-07T16-13-09Z").Build();
        _redis = new RedisBuilder("redis:8.8.0").Build();
        await Task.WhenAll(_minio.StartAsync(), _redis.StartAsync());

        // The test host (WebApplicationFactory) and this test class both run on the same
        // machine as the test process, not inside a container, so there's no internal-vs-public
        // endpoint split here like the real docker-compose deployment has — both point at the
        // same Testcontainers-assigned host-reachable address.
        MinioPublicEndpoint = StripScheme(_minio.GetConnectionString());
        MinioAccessKey = _minio.GetAccessKey();
        MinioSecretKey = _minio.GetSecretKey();

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:CatalogDatabase"] = _postgres.GetConnectionString(),
                    ["Kafka:BootstrapServers"] = _kafka.GetBootstrapAddress(),
                    ["Kafka:SchemaRegistryUrl"] = $"http://localhost:{_schemaRegistry.GetMappedPublicPort(SchemaRegistryPort)}",
                    ["Jwt:Key"] = JwtKey,
                    ["Jwt:Issuer"] = JwtIssuer,
                    ["Jwt:Audience"] = JwtAudience,
                    ["Minio:Endpoint"] = MinioPublicEndpoint,
                    ["Minio:PublicBaseUrl"] = MinioPublicEndpoint,
                    ["Minio:AccessKey"] = MinioAccessKey,
                    ["Minio:SecretKey"] = MinioSecretKey,
                    ["Minio:BucketName"] = "catalog-product-images-test",
                    ["Minio:UseSSL"] = "false",
                    ["Redis:ConnectionString"] = _redis.GetConnectionString(),
                    ["Redis:DefaultTtlSeconds"] = "300"
                });
            });
        });

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    private async Task WaitForSchemaRegistryAsync()
    {
        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri($"http://localhost:{_schemaRegistry.GetMappedPublicPort(SchemaRegistryPort)}")
        };

        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await httpClient.GetAsync("/subjects");
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Schema Registry not accepting connections yet; retry.
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        throw new TimeoutException("Schema Registry did not become ready within the allotted time.");
    }

    private static string StripScheme(string endpoint) =>
        endpoint.Replace("http://", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("https://", string.Empty, StringComparison.OrdinalIgnoreCase)
                .TrimEnd('/');

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _redis.DisposeAsync();
        await _minio.DisposeAsync();
        await _schemaRegistry.DisposeAsync();
        await _kafka.DisposeAsync();
        await _postgres.DisposeAsync();
        await _network.DeleteAsync();
    }
}
