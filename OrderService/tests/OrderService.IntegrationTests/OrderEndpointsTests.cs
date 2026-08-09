using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using OrderService.Application.Dtos;
using OrderService.Application.Interfaces;
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

        var payResponse = await client.PostAsync($"/api/orders/{order!.Id}/pay", null);
        Assert.Equal(HttpStatusCode.OK, payResponse.StatusCode);
        var paid = await payResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal("Paid", paid!.Status);

        var shipResponse = await client.PostAsync($"/api/orders/{order.Id}/ship", null);
        Assert.Equal(HttpStatusCode.OK, shipResponse.StatusCode);
        var shipped = await shipResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal("Shipped", shipped!.Status);

        var reservationId = _fixture.InventoryServiceClient.Reservations.Keys.Single(id =>
            _fixture.InventoryServiceClient.Reservations[id].ProductId == productId);
        Assert.Equal("Committed", _fixture.InventoryServiceClient.Reservations[reservationId].Status);

        var deliverResponse = await client.PostAsync($"/api/orders/{order.Id}/deliver", null);
        Assert.Equal(HttpStatusCode.OK, deliverResponse.StatusCode);
        var delivered = await deliverResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal("Delivered", delivered!.Status);
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
