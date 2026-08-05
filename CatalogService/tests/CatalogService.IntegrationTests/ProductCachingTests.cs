using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CatalogService.Application.Commands;
using CatalogService.Application.Dtos;
using CatalogService.Domain.Entities;
using CatalogService.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CatalogService.IntegrationTests;

[Collection("CatalogApi")]
public class ProductCachingTests
{
    private readonly HttpClient _client;
    private readonly HttpClient _authedClient;
    private readonly IDatabase _redisDb;

    public ProductCachingTests(CatalogApiFixture fixture)
    {
        _client = fixture.Factory.CreateClient();

        _authedClient = fixture.Factory.CreateClient();
        _authedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokenFactory.CreateAccessToken());

        var connectionMultiplexer = fixture.Factory.Services.GetRequiredService<IConnectionMultiplexer>();
        _redisDb = connectionMultiplexer.GetDatabase();
    }

    [Fact]
    public async Task Get_Product_PopulatesRedisCache()
    {
        var category = await CreateCategoryAsync();
        var product = await CreateProductAsync(category.Id);

        var response = await _client.GetAsync($"/api/products/{product.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var cached = await _redisDb.StringGetAsync($"catalog:product:{product.Id}");
        Assert.True(cached.HasValue);
    }

    [Fact]
    public async Task Update_Product_InvalidatesCacheAndSubsequentGetReflectsChange()
    {
        var category = await CreateCategoryAsync();
        var product = await CreateProductAsync(category.Id);
        var cacheKey = $"catalog:product:{product.Id}";

        var firstGet = await _client.GetAsync($"/api/products/{product.Id}");
        Assert.Equal(HttpStatusCode.OK, firstGet.StatusCode);
        Assert.True((await _redisDb.StringGetAsync(cacheKey)).HasValue);

        var updateResponse = await _authedClient.PutAsJsonAsync(
            $"/api/products/{product.Id}",
            new UpdateProductCommand(product.Id, "Renamed Widget", "Updated", 42m, ProductStatus.Active, category.Id));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        Assert.False((await _redisDb.StringGetAsync(cacheKey)).HasValue);

        var secondGet = await _client.GetAsync($"/api/products/{product.Id}");
        var updated = await secondGet.Content.ReadFromJsonAsync<ProductDto>();
        Assert.Equal("Renamed Widget", updated!.Name);
        Assert.Equal(42m, updated.Price);

        var recached = await _redisDb.StringGetAsync(cacheKey);
        Assert.True(recached.HasValue);
        Assert.Contains("Renamed Widget", recached.ToString());
    }

    private async Task<CategoryDto> CreateCategoryAsync()
    {
        var response = await _authedClient.PostAsJsonAsync("/api/categories", new CreateCategoryCommand($"Category-{Guid.NewGuid():N}", null, null));
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }

    private async Task<ProductDto> CreateProductAsync(Guid categoryId)
    {
        var response = await _authedClient.PostAsJsonAsync("/api/products", new CreateProductCommand("Widget", null, $"SKU-{Guid.NewGuid():N}", 9.99m, categoryId));
        return (await response.Content.ReadFromJsonAsync<ProductDto>())!;
    }
}
