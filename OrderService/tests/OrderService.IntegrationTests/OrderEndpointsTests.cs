using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using OrderService.Application.Dtos;
using OrderService.Application.Interfaces;
using OrderService.Infrastructure.Messaging.Schemas;
using OrderService.IntegrationTests.Fixtures;

namespace OrderService.IntegrationTests;

[Collection("OrderApi")]
public class OrderEndpointsTests
{
    private readonly OrderApiFixture _fixture;

    public OrderEndpointsTests(OrderApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Checkout_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _fixture.Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/orders/checkout", new { shippingAddress = "1 Main St" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_EmptyCart_ReturnsBadRequest()
    {
        var userId = Guid.NewGuid();
        var client = AuthedClient(userId);
        _fixture.CartServiceClient.Carts[userId] = new CartSnapshot(userId, []);

        var response = await client.PostAsJsonAsync("/api/orders/checkout", new { shippingAddress = "1 Main St" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_WithStockAvailable_CreatesOrderReservesStockAndClearsCart()
    {
        var userId = Guid.NewGuid();
        var client = AuthedClient(userId);
        var productId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        _fixture.CartServiceClient.Carts[userId] = new CartSnapshot(userId,
            [new CartLineSnapshot(productId, "SKU-1", "Widget", 12.50m, 2)]);
        _fixture.InventoryServiceClient.StockByProduct[productId] = [new LocationStockSnapshot(locationId, 10)];

        var response = await client.PostAsJsonAsync("/api/orders/checkout", new { shippingAddress = "1 Main St" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal(userId, order!.UserId);
        Assert.Equal("Pending", order.Status);
        Assert.Equal(25.00m, order.Subtotal);
        Assert.Contains(_fixture.InventoryServiceClient.Reservations.Values, r => r.ProductId == productId && r.Quantity == 2);
        Assert.Contains(userId, _fixture.CartServiceClient.ClearedUserIds);
    }

    [Fact]
    public async Task FullLifecycle_CheckoutPayShipDeliver_TransitionsCorrectlyAndCommitsReservation()
    {
        var userId = Guid.NewGuid();
        var client = AuthedClient(userId);
        var productId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        _fixture.CartServiceClient.Carts[userId] = new CartSnapshot(userId,
            [new CartLineSnapshot(productId, "SKU-2", "Gadget", 20m, 1)]);
        _fixture.InventoryServiceClient.StockByProduct[productId] = [new LocationStockSnapshot(locationId, 5)];

        var checkoutResponse = await client.PostAsJsonAsync("/api/orders/checkout", new { shippingAddress = "1 Main St" });
        var order = await checkoutResponse.Content.ReadFromJsonAsync<OrderDto>();

        var payResponse = await client.PostAsJsonAsync($"/api/orders/{order!.Id}/pay", new { paymentMethodId = "pm_card_visa" });
        Assert.Equal(HttpStatusCode.OK, payResponse.StatusCode);
        var paid = await payResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal("Paid", paid!.Status);
        Assert.Contains(_fixture.PaymentServiceClient.Charges, c => c.OrderId == order.Id && c.Amount == 20m && c.PaymentMethodId == "pm_card_visa");

        // Ship/Deliver are no longer HTTP endpoints — they're driven by consuming ShippingService's
        // shipment.dispatched.v1/shipment.delivered.v1 events, so this test publishes real Avro
        // messages onto those topics (same shape ShippingService's own producer uses) and polls the
        // order until OrderService's own consumers pick them up and advance the status, proving the
        // wiring end to end rather than calling the handler logic directly.
        await PublishShipmentDispatchedAsync(order.Id);
        var shipped = await PollUntilStatusAsync(client, order.Id, "Shipped", TimeSpan.FromSeconds(30));
        Assert.Equal("Shipped", shipped.Status);

        var reservationId = _fixture.InventoryServiceClient.Reservations.Keys.Single(id =>
            _fixture.InventoryServiceClient.Reservations[id].ProductId == productId);
        Assert.Equal("Committed", _fixture.InventoryServiceClient.Reservations[reservationId].Status);

        await PublishShipmentDeliveredAsync(order.Id);
        var delivered = await PollUntilStatusAsync(client, order.Id, "Delivered", TimeSpan.FromSeconds(30));
        Assert.Equal("Delivered", delivered.Status);

        var refundResponse = await client.PostAsync($"/api/orders/{order.Id}/refund", null);
        Assert.Equal(HttpStatusCode.OK, refundResponse.StatusCode);
        var refunded = await refundResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal("Refunded", refunded!.Status);
        Assert.Contains(order.Id, _fixture.PaymentServiceClient.RefundedOrderIds);
    }

    private async Task PublishShipmentDispatchedAsync(Guid orderId)
    {
        using var schemaRegistryClient = new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = _fixture.SchemaRegistryUrl });
        var producerConfig = new ProducerConfig { BootstrapServers = _fixture.KafkaBootstrapServers };

        using var producer = new ProducerBuilder<string, ShipmentDispatchedAvro>(producerConfig)
            .SetValueSerializer(new AvroSerializer<ShipmentDispatchedAvro>(schemaRegistryClient))
            .Build();

        await producer.ProduceAsync("shipment.dispatched.v1", new Message<string, ShipmentDispatchedAvro>
        {
            Key = orderId.ToString(),
            Value = new ShipmentDispatchedAvro
            {
                ShipmentId = Guid.NewGuid().ToString(),
                OrderId = orderId.ToString(),
                UserId = Guid.NewGuid().ToString(),
                CarrierName = "USPS",
                TrackingNumber = "EZ2000000002",
                DispatchedAt = DateTime.UtcNow
            }
        });
        producer.Flush(TimeSpan.FromSeconds(5));
    }

    private async Task PublishShipmentDeliveredAsync(Guid orderId)
    {
        using var schemaRegistryClient = new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = _fixture.SchemaRegistryUrl });
        var producerConfig = new ProducerConfig { BootstrapServers = _fixture.KafkaBootstrapServers };

        using var producer = new ProducerBuilder<string, ShipmentDeliveredAvro>(producerConfig)
            .SetValueSerializer(new AvroSerializer<ShipmentDeliveredAvro>(schemaRegistryClient))
            .Build();

        await producer.ProduceAsync("shipment.delivered.v1", new Message<string, ShipmentDeliveredAvro>
        {
            Key = orderId.ToString(),
            Value = new ShipmentDeliveredAvro
            {
                ShipmentId = Guid.NewGuid().ToString(),
                OrderId = orderId.ToString(),
                UserId = Guid.NewGuid().ToString(),
                DeliveredAt = DateTime.UtcNow
            }
        });
        producer.Flush(TimeSpan.FromSeconds(5));
    }

    private static async Task<OrderDto> PollUntilStatusAsync(HttpClient client, Guid orderId, string expectedStatus, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            var response = await client.GetAsync($"/api/orders/{orderId}");
            var order = await response.Content.ReadFromJsonAsync<OrderDto>();
            if (order?.Status == expectedStatus)
            {
                return order;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new TimeoutException($"Order {orderId} did not reach status {expectedStatus} within {timeout}.");
    }

    [Fact]
    public async Task Pay_DeclinedCard_ReturnsBadRequestAndLeavesOrderPending()
    {
        var userId = Guid.NewGuid();
        var client = AuthedClient(userId);
        var productId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        _fixture.CartServiceClient.Carts[userId] = new CartSnapshot(userId,
            [new CartLineSnapshot(productId, "SKU-5", "Whatsit", 30m, 1)]);
        _fixture.InventoryServiceClient.StockByProduct[productId] = [new LocationStockSnapshot(locationId, 5)];

        var checkoutResponse = await client.PostAsJsonAsync("/api/orders/checkout", new { shippingAddress = "1 Main St" });
        var order = await checkoutResponse.Content.ReadFromJsonAsync<OrderDto>();
        _fixture.PaymentServiceClient.DeclinedOrderIds.Add(order!.Id);

        var payResponse = await client.PostAsJsonAsync($"/api/orders/{order.Id}/pay", new { paymentMethodId = "pm_card_visa_chargeDeclined" });

        Assert.Equal(HttpStatusCode.BadRequest, payResponse.StatusCode);
        var getResponse = await client.GetAsync($"/api/orders/{order.Id}");
        var stillPending = await getResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal("Pending", stillPending!.Status);
    }

    [Fact]
    public async Task Cancel_PendingOrder_ReleasesReservationAndCancels()
    {
        var userId = Guid.NewGuid();
        var client = AuthedClient(userId);
        var productId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        _fixture.CartServiceClient.Carts[userId] = new CartSnapshot(userId,
            [new CartLineSnapshot(productId, "SKU-3", "Doohickey", 8m, 1)]);
        _fixture.InventoryServiceClient.StockByProduct[productId] = [new LocationStockSnapshot(locationId, 5)];

        var checkoutResponse = await client.PostAsJsonAsync("/api/orders/checkout", new { shippingAddress = "1 Main St" });
        var order = await checkoutResponse.Content.ReadFromJsonAsync<OrderDto>();

        var cancelResponse = await client.PostAsync($"/api/orders/{order!.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal("Cancelled", cancelled!.Status);

        var reservationId = _fixture.InventoryServiceClient.Reservations.Keys.Single(id =>
            _fixture.InventoryServiceClient.Reservations[id].ProductId == productId);
        Assert.Equal("Released", _fixture.InventoryServiceClient.Reservations[reservationId].Status);
    }

    [Fact]
    public async Task Get_OrderOwnedByDifferentUser_ReturnsNotFound()
    {
        var ownerId = Guid.NewGuid();
        var ownerClient = AuthedClient(ownerId);
        var productId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        _fixture.CartServiceClient.Carts[ownerId] = new CartSnapshot(ownerId,
            [new CartLineSnapshot(productId, "SKU-4", "Thingamajig", 15m, 1)]);
        _fixture.InventoryServiceClient.StockByProduct[productId] = [new LocationStockSnapshot(locationId, 5)];

        var checkoutResponse = await ownerClient.PostAsJsonAsync("/api/orders/checkout", new { shippingAddress = "1 Main St" });
        var order = await checkoutResponse.Content.ReadFromJsonAsync<OrderDto>();

        var strangerClient = AuthedClient(Guid.NewGuid());
        var response = await strangerClient.GetAsync($"/api/orders/{order!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private HttpClient AuthedClient(Guid userId)
    {
        var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokenFactory.CreateAccessToken(userId));
        return client;
    }
}
