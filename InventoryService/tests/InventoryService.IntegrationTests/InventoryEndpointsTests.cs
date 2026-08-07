using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using InventoryService.Application.Commands;
using InventoryService.Application.Dtos;
using InventoryService.IntegrationTests.Fixtures;

namespace InventoryService.IntegrationTests;

[Collection("InventoryApi")]
public class InventoryEndpointsTests
{
    private readonly HttpClient _client;
    private readonly HttpClient _authedClient;

    public InventoryEndpointsTests(InventoryApiFixture fixture)
    {
        _client = fixture.Factory.CreateClient();

        _authedClient = fixture.Factory.CreateClient();
        _authedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokenFactory.CreateAccessToken());
    }

    [Fact]
    public async Task Adjust_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/inventory/adjust", new AdjustStockCommand(Guid.NewGuid(), Guid.NewGuid(), 5, "restock"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Adjust_NewInventoryRecord_CreatesItWithPositiveOnHand()
    {
        var location = await CreateLocationAsync();
        var productId = Guid.NewGuid();

        var response = await _authedClient.PostAsJsonAsync("/api/inventory/adjust", new AdjustStockCommand(productId, location.Id, 25, "initial stock"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = await response.Content.ReadFromJsonAsync<InventoryItemDto>();
        Assert.Equal(25, item!.OnHand);
        Assert.Equal(25, item.Available);
    }

    [Fact]
    public async Task Adjust_NegativeBeyondOnHand_ReturnsBadRequest()
    {
        var location = await CreateLocationAsync();
        var productId = Guid.NewGuid();
        await _authedClient.PostAsJsonAsync("/api/inventory/adjust", new AdjustStockCommand(productId, location.Id, 5, "seed"));

        var response = await _authedClient.PostAsJsonAsync("/api/inventory/adjust", new AdjustStockCommand(productId, location.Id, -10, "over-correction"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ReserveReleaseCommit_FullLifecycle_UpdatesStockCorrectly()
    {
        var location = await CreateLocationAsync();
        var productId = Guid.NewGuid();
        await _authedClient.PostAsJsonAsync("/api/inventory/adjust", new AdjustStockCommand(productId, location.Id, 20, "seed"));

        var reserveResponse = await _authedClient.PostAsJsonAsync(
            "/api/inventory/reservations",
            new ReserveStockCommand(productId, location.Id, 8, "order-1"));
        Assert.Equal(HttpStatusCode.Created, reserveResponse.StatusCode);
        var reservation = await reserveResponse.Content.ReadFromJsonAsync<StockReservationDto>();

        var afterReserve = await _client.GetAsync($"/api/inventory/{productId}/{location.Id}");
        var itemAfterReserve = await afterReserve.Content.ReadFromJsonAsync<InventoryItemDto>();
        Assert.Equal(20, itemAfterReserve!.OnHand);
        Assert.Equal(8, itemAfterReserve.Reserved);
        Assert.Equal(12, itemAfterReserve.Available);

        var commitResponse = await _authedClient.PostAsync($"/api/inventory/reservations/{reservation!.Id}/commit", null);
        Assert.Equal(HttpStatusCode.OK, commitResponse.StatusCode);
        var committed = await commitResponse.Content.ReadFromJsonAsync<StockReservationDto>();
        Assert.Equal("Committed", committed!.Status);

        var afterCommit = await _client.GetAsync($"/api/inventory/{productId}/{location.Id}");
        var itemAfterCommit = await afterCommit.Content.ReadFromJsonAsync<InventoryItemDto>();
        Assert.Equal(12, itemAfterCommit!.OnHand);
        Assert.Equal(0, itemAfterCommit.Reserved);
    }

    [Fact]
    public async Task Reserve_InsufficientStock_ReturnsBadRequest()
    {
        var location = await CreateLocationAsync();
        var productId = Guid.NewGuid();
        await _authedClient.PostAsJsonAsync("/api/inventory/adjust", new AdjustStockCommand(productId, location.Id, 3, "seed"));

        var response = await _authedClient.PostAsJsonAsync(
            "/api/inventory/reservations",
            new ReserveStockCommand(productId, location.Id, 10, "order-2"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Release_ActiveReservation_ReturnsStockToAvailable()
    {
        var location = await CreateLocationAsync();
        var productId = Guid.NewGuid();
        await _authedClient.PostAsJsonAsync("/api/inventory/adjust", new AdjustStockCommand(productId, location.Id, 10, "seed"));

        var reserveResponse = await _authedClient.PostAsJsonAsync(
            "/api/inventory/reservations",
            new ReserveStockCommand(productId, location.Id, 4, "order-3"));
        var reservation = await reserveResponse.Content.ReadFromJsonAsync<StockReservationDto>();

        var releaseResponse = await _authedClient.PostAsync($"/api/inventory/reservations/{reservation!.Id}/release", null);
        Assert.Equal(HttpStatusCode.OK, releaseResponse.StatusCode);

        var afterRelease = await _client.GetAsync($"/api/inventory/{productId}/{location.Id}");
        var item = await afterRelease.Content.ReadFromJsonAsync<InventoryItemDto>();
        Assert.Equal(10, item!.OnHand);
        Assert.Equal(0, item.Reserved);
        Assert.Equal(10, item.Available);
    }

    private async Task<LocationDto> CreateLocationAsync()
    {
        var response = await _authedClient.PostAsJsonAsync("/api/locations", new CreateLocationCommand($"Location-{Guid.NewGuid():N}", $"WH-{Guid.NewGuid():N}"[..11]));
        return (await response.Content.ReadFromJsonAsync<LocationDto>())!;
    }
}
