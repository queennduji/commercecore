using System.Net.Http.Headers;
using System.Net.Http.Json;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using ShippingService.Application.Dtos;
using ShippingService.Infrastructure.Messaging.Schemas;
using ShippingService.IntegrationTests.Fixtures;

namespace ShippingService.IntegrationTests;

/// <summary>
/// Proves the cross-service, event-driven creation flow end to end: a real Avro message is
/// published onto order.paid.v1 (the topic OrderService owns) using the same producer/schema shape
/// OrderService uses, and this service's own OrderPaidConsumer BackgroundService – already running
/// inside the WebApplicationFactory host – is left to pick it up and create the Shipment on its
/// own, with no direct call into the application under test. Mirrors InventoryService's
/// ProductCreatedConsumerTests.
/// </summary>
[Collection("ShippingApi")]
public class OrderPaidConsumerTests
{
    private readonly ShippingApiFixture _fixture;

    public OrderPaidConsumerTests(ShippingApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task OrderPaidEvent_AutoCreatesAwaitingFulfillmentShipment()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await PublishOrderPaidAsync(orderId, userId, "42 Integration Test Way");

        var client = AuthedClient(userId);
        var shipment = await PollUntilShipmentExistsAsync(client, orderId, TimeSpan.FromSeconds(30));

        Assert.Equal(orderId, shipment.OrderId);
        Assert.Equal(userId, shipment.UserId);
        Assert.Equal("42 Integration Test Way", shipment.ShippingAddress);
        Assert.Equal("AwaitingFulfillment", shipment.Status);
    }

    private async Task PublishOrderPaidAsync(Guid orderId, Guid userId, string shippingAddress)
    {
        using var schemaRegistryClient = new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = _fixture.SchemaRegistryUrl });
        var producerConfig = new ProducerConfig { BootstrapServers = _fixture.KafkaBootstrapServers };

        using var producer = new ProducerBuilder<string, OrderPaidAvro>(producerConfig)
            .SetValueSerializer(new AvroSerializer<OrderPaidAvro>(schemaRegistryClient))
            .Build();

        var message = new Message<string, OrderPaidAvro>
        {
            Key = orderId.ToString(),
            Value = new OrderPaidAvro
            {
                OrderId = orderId.ToString(),
                UserId = userId.ToString(),
                PaidAt = DateTime.UtcNow,
                ShippingAddress = shippingAddress
            }
        };

        await producer.ProduceAsync("order.paid.v1", message);
        producer.Flush(TimeSpan.FromSeconds(5));
    }

    private static async Task<ShipmentDto> PollUntilShipmentExistsAsync(HttpClient client, Guid orderId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            var response = await client.GetAsync($"/api/shipments/order/{orderId}");
            if (response.IsSuccessStatusCode)
            {
                return (await response.Content.ReadFromJsonAsync<ShipmentDto>())!;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new TimeoutException($"A shipment for order {orderId} was not created within {timeout}.");
    }

    private HttpClient AuthedClient(Guid userId)
    {
        var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokenFactory.CreateAccessToken(userId));
        return client;
    }
}
