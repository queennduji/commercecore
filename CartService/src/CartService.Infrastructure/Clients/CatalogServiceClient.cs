using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CartService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CartService.Infrastructure.Clients;

public class CatalogServiceClient : ICatalogServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CatalogServiceClient> _logger;

    public CatalogServiceClient(HttpClient httpClient, ILogger<CatalogServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CatalogProductSnapshot?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync($"/api/products/{productId}", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to reach CatalogService for product {ProductId}", productId);
            return null;
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("CatalogService returned {StatusCode} for product {ProductId}", response.StatusCode, productId);
            return null;
        }

        var product = await response.Content.ReadFromJsonAsync<ProductResponse>(cancellationToken: cancellationToken);
        return product is null
            ? null
            : new CatalogProductSnapshot(product.Id, product.Sku, product.Name, product.Price, product.Status);
    }

    // Mirrors only the fields this service needs from CatalogService's ProductDto — property names
    // match the API's camelCase JSON via [JsonPropertyName] rather than relying on case-insensitive
    // matching, so a rename on either side fails loudly instead of silently deserializing nulls.
    private class ProductResponse
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("sku")]
        public string Sku { get; set; } = string.Empty;

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }
}
