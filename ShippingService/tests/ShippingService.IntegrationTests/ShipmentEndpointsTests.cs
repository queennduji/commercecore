using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using ShippingService.Application.Dtos;
using ShippingService.Infrastructure.Messaging.Schemas;
using ShippingService.IntegrationTests.Fixtures;

namespace ShippingService.IntegrationTests;

[Collection("ShippingApi")]
public class ShipmentEndpointsTests
{
    private readonly ShippingApiFixture _fixture;

    public ShipmentEndpointsTests(ShippingApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Get_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _fixture.Factory.CreateClient();

        var response = await client.GetAsync($"/api/shipments/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Dispatch_AwaitingFulfillmentShipment_CreatesTrackerAndReturnsDispatched()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var client = AuthedClient(userId);
        var shipment = await CreateShipmentViaOrderPaidAsync(orderId, userId, client);

        var response = await client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/dispatch", new { carrier = "USPS", trackingCode = "EZ2000000002" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dispatched = await response.Content.ReadFromJsonAsync<ShipmentDto>();
        Assert.Equal("Dispatched", dispatched!.Status);
        Assert.Equal("USPS", dispatched.CarrierName);
        Assert.Equal("EZ2000000002", dispatched.TrackingNumber);
        Assert.Contains(_fixture.CarrierGateway.Creates, c => c.TrackingCode == "EZ2000000002" && c.Carrier == "USPS");
    }

    [Fact]
    public async Task Dispatch_AlreadyDispatchedShipment_ReturnsBadRequest()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var client = AuthedClient(userId);
        var shipment = await CreateShipmentViaOrderPaidAsync(orderId, userId, client);
        await client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/dispatch", new { carrier = "USPS", trackingCode = "EZ2000000002" });

        var response = await client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/dispatch", new { carrier = "USPS", trackingCode = "EZ2000000002" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RefreshTracking_CarrierReportsDelivered_TransitionsToDelivered()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var client = AuthedClient(userId);
        var shipment = await CreateShipmentViaOrderPaidAsync(orderId, userId, client);
        var dispatchResponse = await client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/dispatch", new { carrier = "USPS", trackingCode = "EZ2000000002" });
        var dispatched = await dispatchResponse.Content.ReadFromJsonAsync<ShipmentDto>();

        // The fake gateway records whatever status was set at tracker-creation time; simulate the
        // carrier having since progressed by overwriting it directly, then refresh.
        var providerTrackerId = _fixture.CarrierGateway.StatusByProviderTrackerId.Keys.Last();
        _fixture.CarrierGateway.StatusByProviderTrackerId[providerTrackerId] = "delivered";

        var response = await client.PostAsync($"/api/shipments/{shipment.Id}/refresh-tracking", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var refreshed = await response.Content.ReadFromJsonAsync<ShipmentDto>();
        Assert.Equal("Delivered", refreshed!.Status);
        Assert.NotNull(refreshed.DeliveredAt);
    }

    [Fact]
    public async Task Get_ShipmentOwnedByDifferentUser_ReturnsNotFound()
    {
        var ownerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var ownerClient = AuthedClient(ownerId);
        var shipment = await CreateShipmentViaOrderPaidAsync(orderId, ownerId, ownerClient);

        var strangerClient = AuthedClient(Guid.NewGuid());
        var response = await strangerClient.GetAsync($"/api/shipments/{shipment.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<ShipmentDto> CreateShipmentViaOrderPaidAsync(Guid orderId, Guid userId, HttpClient client)
    {
        using var schemaRegistryClient = new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = _fixture.SchemaRegistryUrl });
        var producerConfig = new ProducerConfig { BootstrapServers = _fixture.KafkaBootstrapServers };

        using var producer = new ProducerBuilder<string, OrderPaidAvro>(producerConfig)
            .SetValueSerializer(new AvroSerializer<OrderPaidAvro>(schemaRegistryClient))
            .Build();

        await producer.ProduceAsync("order.paid.v1", new Message<string, OrderPaidAvro>
        {
            Key = orderId.ToString(),
            Value = new OrderPaidAvro
            {
                OrderId = orderId.ToString(),
                UserId = userId.ToString(),
                PaidAt = DateTime.UtcNow,
                ShippingAddress = "1 Main St"
            }
        });
        producer.Flush(TimeSpan.FromSeconds(5));

        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            var response = await client.GetAsync($"/api/shipments/order/{orderId}");
            if (response.IsSuccessStatusCode)
            {
                return (await response.Content.ReadFromJsonAsync<ShipmentDto>())!;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new TimeoutException($"A shipment for order {orderId} was not created within the allotted time.");
    }

    private HttpClient AuthedClient(Guid userId)
    {
        var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokenFactory.CreateAccessToken(userId));
        return client;
    }
}
