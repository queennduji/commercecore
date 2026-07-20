using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CatalogService.Application.Commands;
using CatalogService.Application.Dtos;
using CatalogService.Domain.Entities;
using CatalogService.IntegrationTests.Fixtures;

namespace CatalogService.IntegrationTests;

[Collection("CatalogApi")]
public class ProductsEndpointsTests
{
    private readonly HttpClient _client;
    private readonly HttpClient _authedClient;

    public ProductsEndpointsTests(CatalogApiFixture fixture)
    {
        _client = fixture.Factory.CreateClient();

        _authedClient = fixture.Factory.CreateClient();
        _authedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokenFactory.CreateAccessToken());
    }

    [Fact]
    public async Task Create_WithoutAuth_ReturnsUnauthorized()
    {
        var category = await CreateCategoryAsync();

        var response = await _client.PostAsJsonAsync("/api/products", new CreateProductCommand("Widget", null, $"SKU-{Guid.NewGuid():N}", 9.99m, category.Id));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithAuth_ReturnsCreated()
    {
        var category = await CreateCategoryAsync();

        var response = await _authedClient.PostAsJsonAsync("/api/products", new CreateProductCommand("Widget", "A widget", $"SKU-{Guid.NewGuid():N}", 9.99m, category.Id));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotNull(product);
        Assert.Equal(ProductStatus.Draft.ToString(), product!.Status);
    }

    [Fact]
    public async Task Update_ExistingProduct_ReturnsOk()
    {
        var category = await CreateCategoryAsync();
        var product = await CreateProductAsync(category.Id);

        var response = await _authedClient.PutAsJsonAsync(
            $"/api/products/{product.Id}",
            new UpdateProductCommand(product.Id, "Updated Widget", "Updated", 15m, ProductStatus.Active, category.Id));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.Equal("Updated Widget", updated!.Name);
        Assert.Equal(ProductStatus.Active.ToString(), updated.Status);
    }

    [Fact]
    public async Task Delete_ExistingProduct_ArchivesRatherThanHardDeleting()
    {
        var category = await CreateCategoryAsync();
        var product = await CreateProductAsync(category.Id);

        var deleteResponse = await _authedClient.DeleteAsync($"/api/products/{product.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/products/{product.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var archived = await getResponse.Content.ReadFromJsonAsync<ProductDto>();
        Assert.Equal(ProductStatus.Archived.ToString(), archived!.Status);
    }

    [Fact]
    public async Task Get_UnknownProduct_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/products/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_FiltersByCategory()
    {
        var category = await CreateCategoryAsync();
        await CreateProductAsync(category.Id);

        var response = await _client.GetAsync($"/api/products?categoryId={category.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        Assert.NotEmpty(page!.Items);
        Assert.All(page.Items, p => Assert.Equal(category.Id, p.CategoryId));
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
