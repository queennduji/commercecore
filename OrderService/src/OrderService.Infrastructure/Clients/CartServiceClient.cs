using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using OrderService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace OrderService.Infrastructure.Clients;

/// <summary>Synchronous HTTP call to CartService, same pattern as CartService's own call into
/// CatalogService. Checkout always targets the caller's own cart (Id == UserId).</summary>
public class CartServiceClient : ICartServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CartServiceClient> _logger;

    public CartServiceClient(HttpClient httpClient, ILogger<CartServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CartSnapshot?> GetCartAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync($"/api/carts/{userId}", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to reach CartService for cart {UserId}", userId);
            return null;
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("CartService returned {StatusCode} for cart {UserId}", response.StatusCode, userId);
            return null;
        }

        var cart = await response.Content.ReadFromJsonAsync<CartResponse>(cancellationToken: cancellationToken);
        if (cart is null)
        {
            return null;
        }

        var items = cart.Items
            .Select(i => new CartLineSnapshot(i.ProductId, i.Sku, i.Name, i.UnitPrice, i.Quantity))
            .ToList();

        return new CartSnapshot(cart.Id, items);
    }

    public async Task ClearCartAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _httpClient.DeleteAsync($"/api/carts/{userId}", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to clear cart {UserId} in CartService", userId);
        }
    }

    // Mirrors only the fields this service needs from CartService's CartDto/CartItemDto.
    private class CartResponse
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("items")]
        public List<CartItemResponse> Items { get; set; } = [];
    }

    private class CartItemResponse
    {
        [JsonPropertyName("productId")]
        public Guid ProductId { get; set; }

        [JsonPropertyName("sku")]
        public string Sku { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("unitPrice")]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
    }
}
