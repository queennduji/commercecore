using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CatalogService.Application.Commands;
using CatalogService.Application.Dtos;
using CatalogService.IntegrationTests.Fixtures;

namespace CatalogService.IntegrationTests;

[Collection("CatalogApi")]
public class CategoriesEndpointsTests
{
    private readonly HttpClient _client;
    private readonly HttpClient _authedClient;

    public CategoriesEndpointsTests(CatalogApiFixture fixture)
    {
        _client = fixture.Factory.CreateClient();

        _authedClient = fixture.Factory.CreateClient();
        _authedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokenFactory.CreateAccessToken());
    }

    [Fact]
    public async Task Create_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/categories", new CreateCategoryCommand("Books", null, null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithAuth_ReturnsCreated()
    {
        var response = await _authedClient.PostAsJsonAsync("/api/categories", new CreateCategoryCommand("Electronics", "Gadgets", null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var category = await response.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.NotNull(category);
        Assert.Equal("Electronics", category!.Name);
    }

    [Fact]
    public async Task Get_ExistingCategory_ReturnsOk()
    {
        var created = await CreateCategoryAsync("Home");

        var response = await _client.GetAsync($"/api/categories/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsCategories()
    {
        await CreateCategoryAsync("Toys");

        var response = await _client.GetAsync("/api/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>();
        Assert.NotEmpty(categories!);
    }

    [Fact]
    public async Task Delete_CategoryWithProducts_ReturnsBadRequest()
    {
        var category = await CreateCategoryAsync("Sports");
        await _authedClient.PostAsJsonAsync("/api/products", new CreateProductCommand("Ball", null, $"SKU-{Guid.NewGuid():N}", 5m, category.Id));

        var response = await _authedClient.DeleteAsync($"/api/categories/{category.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_CategoryWithoutProducts_ReturnsNoContent()
    {
        var category = await CreateCategoryAsync("Garden");

        var response = await _authedClient.DeleteAsync($"/api/categories/{category.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private async Task<CategoryDto> CreateCategoryAsync(string name)
    {
        var response = await _authedClient.PostAsJsonAsync("/api/categories", new CreateCategoryCommand(name, null, null));
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }
}
