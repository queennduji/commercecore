using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using InventoryService.Application.Commands;
using InventoryService.Application.Dtos;
using InventoryService.IntegrationTests.Fixtures;

namespace InventoryService.IntegrationTests;

[Collection("InventoryApi")]
public class LocationsEndpointsTests
{
    private readonly HttpClient _client;
    private readonly HttpClient _authedClient;

    public LocationsEndpointsTests(InventoryApiFixture fixture)
    {
        _client = fixture.Factory.CreateClient();

        _authedClient = fixture.Factory.CreateClient();
        _authedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokenFactory.CreateAccessToken());
    }

    [Fact]
    public async Task Create_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/locations", new CreateLocationCommand("East Warehouse", NewCode()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithAuth_ReturnsCreated()
    {
        var response = await _authedClient.PostAsJsonAsync("/api/locations", new CreateLocationCommand("East Warehouse", NewCode()));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var location = await response.Content.ReadFromJsonAsync<LocationDto>();
        Assert.NotNull(location);
        Assert.True(location!.IsActive);
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsBadRequest()
    {
        var code = NewCode();
        await _authedClient.PostAsJsonAsync("/api/locations", new CreateLocationCommand("First", code));

        var response = await _authedClient.PostAsJsonAsync("/api/locations", new CreateLocationCommand("Second", code));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ExistingLocation_ReturnsOk()
    {
        var location = await CreateLocationAsync();

        var response = await _authedClient.PutAsJsonAsync(
            $"/api/locations/{location.Id}",
            new UpdateLocationCommand(location.Id, "Renamed", location.Code, true));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<LocationDto>();
        Assert.Equal("Renamed", updated!.Name);
    }

    [Fact]
    public async Task Deactivate_LocationWithNoStock_ReturnsNoContent()
    {
        var location = await CreateLocationAsync();

        var response = await _authedClient.DeleteAsync($"/api/locations/{location.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync($"/api/locations/{location.Id}");
        var reloaded = await getResponse.Content.ReadFromJsonAsync<LocationDto>();
        Assert.False(reloaded!.IsActive);
    }

    [Fact]
    public async Task Deactivate_LocationWithStock_ReturnsBadRequest()
    {
        var location = await CreateLocationAsync();
        var productId = Guid.NewGuid();

        await _authedClient.PostAsJsonAsync("/api/inventory/adjust", new AdjustStockCommand(productId, location.Id, 5, "seed stock"));

        var response = await _authedClient.DeleteAsync($"/api/locations/{location.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsLocations()
    {
        await CreateLocationAsync();

        var response = await _client.GetAsync("/api/locations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var locations = await response.Content.ReadFromJsonAsync<List<LocationDto>>();
        Assert.NotEmpty(locations!);
    }

    private async Task<LocationDto> CreateLocationAsync()
    {
        var response = await _authedClient.PostAsJsonAsync("/api/locations", new CreateLocationCommand($"Location-{Guid.NewGuid():N}", NewCode()));
        return (await response.Content.ReadFromJsonAsync<LocationDto>())!;
    }

    private static string NewCode() => $"WH-{Guid.NewGuid():N}"[..11];
}
