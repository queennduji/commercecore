using System.Net;
using System.Net.Http.Json;
using CartService.Application.Dtos;
using CartService.Application.Interfaces;
using CartService.IntegrationTests.Fixtures;

namespace CartService.IntegrationTests;

[Collection("CartApi")]
public class CartEndpointsTests
{
    private readonly HttpClient _client;
    private readonly FakeCatalogServiceClient _catalogServiceClient;

    public CartEndpointsTests(CartApiFixture fixture)
    {
        _client = fixture.Factory.CreateClient();
        _catalogServiceClient = fixture.CatalogServiceClient;
    }

    [Fact]
    public async Task Create_ReturnsNewEmptyGuestCart()
    {
        var response = await _client.PostAsync("/api/carts", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var cart = await response.Content.ReadFromJsonAsync<CartDto>();
        Assert.NotEqual(Guid.Empty, cart!.Id);
        Assert.Null(cart.UserId);
        Assert.Empty(cart.Items);
    }

    [Fact]
    public async Task AddItem_KnownActiveProduct_SnapshotsPriceAndReturnsUpdatedCart()
    {
        var cart = await CreateCartAsync();
        var productId = Guid.NewGuid();
        _catalogServiceClient.Products[productId] = new CatalogProductSnapshot(productId, "SKU-1", "Widget", 12.50m, "Active");

        var response = await _client.PostAsJsonAsync($"/api/carts/{cart.Id}/items", new { productId, quantity = 3 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<CartDto>();
        var item = Assert.Single(updated!.Items);
        Assert.Equal(12.50m, item.UnitPrice);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(37.50m, updated.Subtotal);
    }

    [Fact]
    public async Task AddItem_UnknownProduct_ReturnsBadRequest()
    {
        var cart = await CreateCartAsync();

        var response = await _client.PostAsJsonAsync($"/api/carts/{cart.Id}/items", new { productId = Guid.NewGuid(), quantity = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateItemQuantity_ExistingItem_UpdatesQuantity()
    {
        var cart = await CreateCartAsync();
        var productId = Guid.NewGuid();
        _catalogServiceClient.Products[productId] = new CatalogProductSnapshot(productId, "SKU-1", "Widget", 5m, "Active");
        await _client.PostAsJsonAsync($"/api/carts/{cart.Id}/items", new { productId, quantity = 1 });

        var response = await _client.PutAsJsonAsync($"/api/carts/{cart.Id}/items/{productId}", new { quantity = 9 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<CartDto>();
        Assert.Equal(9, updated!.Items.Single().Quantity);
    }

    [Fact]
    public async Task RemoveItem_ExistingItem_RemovesIt()
    {
        var cart = await CreateCartAsync();
        var productId = Guid.NewGuid();
        _catalogServiceClient.Products[productId] = new CatalogProductSnapshot(productId, "SKU-1", "Widget", 5m, "Active");
        await _client.PostAsJsonAsync($"/api/carts/{cart.Id}/items", new { productId, quantity = 1 });

        var response = await _client.DeleteAsync($"/api/carts/{cart.Id}/items/{productId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<CartDto>();
        Assert.Empty(updated!.Items);
    }

    [Fact]
    public async Task Get_UnknownCart_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/carts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Clear_ExistingCart_DeletesItEntirely()
    {
        var cart = await CreateCartAsync();

        var clearResponse = await _client.DeleteAsync($"/api/carts/{cart.Id}");
        Assert.Equal(HttpStatusCode.NoContent, clearResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/carts/{cart.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private async Task<CartDto> CreateCartAsync()
    {
        var response = await _client.PostAsync("/api/carts", null);
        return (await response.Content.ReadFromJsonAsync<CartDto>())!;
    }
}
