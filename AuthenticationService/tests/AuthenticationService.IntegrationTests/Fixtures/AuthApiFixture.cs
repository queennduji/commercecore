using AuthenticationService.Infrastructure.Persistence;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;
using IContainer = DotNet.Testcontainers.Containers.IContainer;

namespace AuthenticationService.IntegrationTests.Fixtures;

public class AuthApiFixture : IAsyncLifetime
{
    private const string KafkaNetworkAlias = "kafka";
    private const string KafkaInternalListener = KafkaNetworkAlias + ":19092";
    private const int SchemaRegistryPort = 8081;

    private INetwork _network = null!;
    private PostgreSqlContainer _postgres = null!;
    private KafkaContainer _kafka = null!;
    private IContainer _schemaRegistry = null!;

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _network = new NetworkBuilder().Build();
        await _network.CreateAsync();

        _postgres = new PostgreSqlBuilder("postgres:16")
            .WithNetwork(_network)
            .WithNetworkAliases("postgres")
            .WithDatabase("authentication_service_test")
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

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:AuthDatabase"] = _postgres.GetConnectionString(),
                    ["Kafka:BootstrapServers"] = _kafka.GetBootstrapAddress(),
                    ["Kafka:SchemaRegistryUrl"] = $"http://localhost:{_schemaRegistry.GetMappedPublicPort(SchemaRegistryPort)}",
                    ["Jwt:Key"] = Convert.ToBase64String(new byte[32]),
                    ["Jwt:Issuer"] = "CommerceCore.AuthenticationService.IntegrationTests",
                    ["Jwt:Audience"] = "CommerceCore.IntegrationTests",
                    // Never actually reached in tests – OTLP export failures are non-fatal at
                    // runtime, so a real collector isn't needed here. Only present because Otel
                    // config is required at startup, same as Jwt above.
                    ["Otel:ServiceName"] = "AuthenticationService.Tests",
                    ["Otel:TracesEndpoint"] = "http://127.0.0.1:1",
                    ["Otel:LogsEndpoint"] = "http://127.0.0.1:1"
                });
            });
        });

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    private async Task WaitForSchemaRegistryAsync()
    {
        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri($"http://localhost:{_schemaRegistry.GetMappedPublicPort(SchemaRegistryPort)}")
        };

        var deadline = DateTime.UtcNow.AddSeconds(120);
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
            catch (TaskCanceledException)
            {
                // Request timed out while Schema Registry was still starting up; retry.
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        throw new TimeoutException("Schema Registry did not become ready within the allotted time.");
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }

        if (_schemaRegistry is not null)
        {
            await _schemaRegistry.DisposeAsync();
        }

        if (_kafka is not null)
        {
            await _kafka.DisposeAsync();
        }

        if (_postgres is not null)
        {
            await _postgres.DisposeAsync();
        }

        if (_network is not null)
        {
            await _network.DeleteAsync();
        }
    }
}
