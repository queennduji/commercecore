using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CatalogService.Application.Commands;
using CatalogService.Application.Dtos;
using CatalogService.Application.Interfaces;
using CatalogService.IntegrationTests.Fixtures;

namespace CatalogService.IntegrationTests;

[Collection("CatalogApi")]
public class ProductImagesEndpointsTests
{
    private readonly CatalogApiFixture _fixture;
    private readonly HttpClient _client;
    private readonly HttpClient _authedClient;

    public ProductImagesEndpointsTests(CatalogApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();

        _authedClient = fixture.Factory.CreateClient();
        _authedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokenFactory.CreateAccessToken());
    }

    [Fact]
    public async Task FullFlow_RequestUploadUrl_UploadDirectly_Attach_Get_Delete()
    {
        var category = await CreateCategoryAsync();
        var product = await CreateProductAsync(category.Id);

        // 1. Request a presigned upload URL.
        var uploadUrlResponse = await _authedClient.PostAsJsonAsync(
            $"/api/products/{product.Id}/images/upload-url",
            new RequestProductImageUploadCommand(product.Id, "photo.png", "image/png"));
        Assert.Equal(HttpStatusCode.OK, uploadUrlResponse.StatusCode);
        var presigned = await uploadUrlResponse.Content.ReadFromJsonAsync<PresignedUploadUrl>();
        Assert.NotNull(presigned);

        // 2. Upload the file bytes directly to MinIO (never through CatalogService).
        using var uploadClient = new HttpClient();
        var fileBytes = "fake-png-bytes"u8.ToArray();
        using var putContent = new ByteArrayContent(fileBytes);
        putContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var putResponse = await uploadClient.PutAsync(presigned!.UploadUrl, putContent);
        Assert.True(putResponse.IsSuccessStatusCode, $"Direct upload to MinIO failed: {putResponse.StatusCode}");

        // 3. Tell CatalogService the upload succeeded so it records the image.
        var attachResponse = await _authedClient.PostAsJsonAsync(
            $"/api/products/{product.Id}/images",
            new AttachProductImageCommand(product.Id, presigned.ObjectKey, 0, true));
        Assert.Equal(HttpStatusCode.OK, attachResponse.StatusCode);
        var image = await attachResponse.Content.ReadFromJsonAsync<ProductImageDto>();
        Assert.NotNull(image);
        Assert.True(image!.IsPrimary);

        // 4. Confirm the image shows up on the product and is publicly fetchable.
        var getResponse = await _client.GetAsync($"/api/products/{product.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var productDto = await getResponse.Content.ReadFromJsonAsync<ProductDto>();
        Assert.Single(productDto!.Images);
        Assert.Equal(image.Url, productDto.Images[0].Url);

        using var publicFetchClient = new HttpClient();
        var publicFetchResponse = await publicFetchClient.GetAsync(image.Url);
        Assert.Equal(HttpStatusCode.OK, publicFetchResponse.StatusCode);

        // 5. Delete the image and confirm it's gone from both the DB and MinIO.
        var deleteResponse = await _authedClient.DeleteAsync($"/api/products/{product.Id}/images/{image.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getAfterDeleteResponse = await _client.GetAsync($"/api/products/{product.Id}");
        var productAfterDelete = await getAfterDeleteResponse.Content.ReadFromJsonAsync<ProductDto>();
        Assert.Empty(productAfterDelete!.Images);

        using var publicFetchAfterDeleteClient = new HttpClient();
        var publicFetchAfterDeleteResponse = await publicFetchAfterDeleteClient.GetAsync(image.Url);
        Assert.Equal(HttpStatusCode.NotFound, publicFetchAfterDeleteResponse.StatusCode);
    }

    [Fact]
    public async Task RequestUploadUrl_WithoutAuth_ReturnsUnauthorized()
    {
        var category = await CreateCategoryAsync();
        var product = await CreateProductAsync(category.Id);

        var response = await _client.PostAsJsonAsync(
            $"/api/products/{product.Id}/images/upload-url",
            new RequestProductImageUploadCommand(product.Id, "photo.png", "image/png"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RequestUploadUrl_NonImageContentType_ReturnsBadRequest()
    {
        var category = await CreateCategoryAsync();
        var product = await CreateProductAsync(category.Id);

        var response = await _authedClient.PostAsJsonAsync(
            $"/api/products/{product.Id}/images/upload-url",
            new RequestProductImageUploadCommand(product.Id, "file.txt", "text/plain"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
