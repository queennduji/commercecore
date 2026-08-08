using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CartService.Application.Dtos;
using CartService.Application.Interfaces;
using CartService.IntegrationTests.Fixtures;

namespace CartService.IntegrationTests;

[Collection("CartApi")]
public class MergeCartEndpointsTests
{
    private readonly CartApiFixture _fixture;
    private readonly HttpClient _client;
    private readonly FakeCatalogServiceClient _catalogServiceClient;

    public MergeCartEndpointsTests(CartApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
        _catalogServiceClient = fixture.CatalogServiceClient;
    }

    [Fact]
    public async Task GetMyCart_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/carts/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyCart_FirstCall_CreatesCartKeyedByUserId()
    {
        var userId = Guid.NewGuid();
        var authedClient = AuthedClient(userId);

        var response = await authedClient.GetAsync("/api/carts/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cart = await response.Content.ReadFromJsonAsync<CartDto>();
        Assert.Equal(userId, cart!.Id);
        Assert.Equal(userId, cart.UserId);
    }

    [Fact]
    public async Task Merge_GuestCartIntoUserCart_CombinesItemsAndDeletesGuestCart()
    {
        var userId = Guid.NewGuid();
        var authedClient = AuthedClient(userId);

        // Shopper adds something to their own (authenticated) cart first.
        var myCartResponse = await authedClient.GetAsync("/api/carts/me");
        var myCart = await myCartResponse.Content.ReadFromJsonAsync<CartDto>();
        var sharedProductId = Guid.NewGuid();
        _catalogServiceClient.Products[sharedProductId] = new CatalogProductSnapshot(sharedProductId, "SKU-SHARED", "Shared Widget", 10m, "Active");
        await authedClient.PostAsJsonAsync($"/api/carts/{myCart!.Id}/items", new { productId = sharedProductId, quantity = 1 });

        // Meanwhile, a guest cart (pre-login) has the same product plus a unique one.
        var guestCartResponse = await _client.PostAsync("/api/carts", null);
        var guestCart = await guestCartResponse.Content.ReadFromJsonAsync<CartDto>();
        var guestOnlyProductId = Guid.NewGuid();
        _catalogServiceClient.Products[guestOnlyProductId] = new CatalogProductSnapshot(guestOnlyProductId, "SKU-GUEST", "Guest-only Widget", 4m, "Active");
        await _client.PostAsJsonAsync($"/api/carts/{guestCart!.Id}/items", new { productId = sharedProductId, quantity = 2 });
        await _client.PostAsJsonAsync($"/api/carts/{guestCart.Id}/items", new { productId = guestOnlyProductId, quantity = 1 });

        var mergeResponse = await authedClient.PostAsJsonAsync("/api/carts/me/merge", new { sourceCartId = guestCart.Id });

        Assert.Equal(HttpStatusCode.OK, mergeResponse.StatusCode);
        var merged = await mergeResponse.Content.ReadFromJsonAsync<CartDto>();
        Assert.Equal(userId, merged!.Id);
        Assert.Equal(2, merged.Items.Count);
        Assert.Equal(3, merged.Items.Single(i => i.ProductId == sharedProductId).Quantity);
        Assert.Equal(1, merged.Items.Single(i => i.ProductId == guestOnlyProductId).Quantity);

        var guestCartAfterMerge = await _client.GetAsync($"/api/carts/{guestCart.Id}");
        Assert.Equal(HttpStatusCode.NotFound, guestCartAfterMerge.StatusCode);
    }

    private HttpClient AuthedClient(Guid userId)
    {
        var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokenFactory.CreateAccessToken(userId));
        return client;
    }
}
