using System.Net.Http.Headers;
using System.Net.Http.Json;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using InventoryService.Application.Commands;
using InventoryService.Application.Dtos;
using InventoryService.Infrastructure.Messaging.Schemas;
using InventoryService.IntegrationTests.Fixtures;

namespace InventoryService.IntegrationTests;

/// <summary>
/// Proves the cross-service, event-driven provisioning flow end to end: a real Avro message is
/// published onto catalog.product-created.v1 (the topic CatalogService owns) using the same
/// producer/schema shape CatalogService uses, and this service's own ProductCreatedConsumer
/// BackgroundService — already running inside the WebApplicationFactory host — is left to pick it
/// up and provision inventory on its own, with no direct call into the application under test.
/// </summary>
[Collection("InventoryApi")]
public class ProductCreatedConsumerTests
{
    private readonly InventoryApiFixture _fixture;
    private readonly HttpClient _client;
    private readonly HttpClient _authedClient;

    public ProductCreatedConsumerTests(InventoryApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();

        _authedClient = fixture.Factory.CreateClient();
        _authedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokenFactory.CreateAccessToken());
    }

    [Fact]
    public async Task ProductCreatedEvent_AutoProvisionsZeroStockAtEveryActiveLocation()
    {
        var locationA = await CreateLocationAsync();
        var locationB = await CreateLocationAsync();
        var productId = Guid.NewGuid();

        await PublishProductCreatedAsync(productId);

        var items = await PollUntilProvisionedAsync(productId, expectedCount: 2, timeout: TimeSpan.FromSeconds(30));

        Assert.Equal(2, items.Count);
        Assert.Contains(items, i => i.LocationId == locationA.Id && i.OnHand == 0 && i.Reserved == 0);
        Assert.Contains(items, i => i.LocationId == locationB.Id && i.OnHand == 0 && i.Reserved == 0);
    }

    private async Task PublishProductCreatedAsync(Guid productId)
    {
        using var schemaRegistryClient = new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = _fixture.SchemaRegistryUrl });
        var producerConfig = new ProducerConfig { BootstrapServers = _fixture.KafkaBootstrapServers };

        using var producer = new ProducerBuilder<string, ProductCreatedAvro>(producerConfig)
            .SetValueSerializer(new AvroSerializer<ProductCreatedAvro>(schemaRegistryClient))
            .Build();

        var message = new Message<string, ProductCreatedAvro>
        {
            Key = productId.ToString(),
            Value = new ProductCreatedAvro
            {
                ProductId = productId.ToString(),
                Name = "Integration Test Widget",
                Sku = $"SKU-{Guid.NewGuid():N}",
                Price = 19.99,
                CategoryId = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow
            }
        };

        await producer.ProduceAsync("catalog.product-created.v1", message);
        producer.Flush(TimeSpan.FromSeconds(5));
    }

    private async Task<List<InventoryItemDto>> PollUntilProvisionedAsync(Guid productId, int expectedCount, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            var response = await _client.GetAsync($"/api/inventory/{productId}");
            var items = await response.Content.ReadFromJsonAsync<List<InventoryItemDto>>() ?? [];
            if (items.Count >= expectedCount)
            {
                return items;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new TimeoutException($"InventoryItem records for product {productId} were not provisioned within {timeout}.");
    }

    private async Task<LocationDto> CreateLocationAsync()
    {
        var response = await _authedClient.PostAsJsonAsync("/api/locations", new CreateLocationCommand($"Location-{Guid.NewGuid():N}", $"WH-{Guid.NewGuid():N}"[..11]));
        return (await response.Content.ReadFromJsonAsync<LocationDto>())!;
    }
}
