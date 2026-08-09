using System.Net.Http.Json;
using System.Text.Json.Serialization;
using OrderService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace OrderService.Infrastructure.Clients;

/// <summary>Synchronous HTTP call to InventoryService: picks a fulfilling location at checkout
/// (the first with enough Available stock) and drives the reserve/release/commit trio as the order
/// moves through its lifecycle.</summary>
public class InventoryServiceClient : IInventoryServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryServiceClient> _logger;

    public InventoryServiceClient(HttpClient httpClient, ILogger<InventoryServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LocationStockSnapshot>> GetStockAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        try
        {
            var items = await _httpClient.GetFromJsonAsync<List<InventoryItemResponse>>($"/api/inventory/{productId}", cancellationToken);
            return items?.Select(i => new LocationStockSnapshot(i.LocationId, i.Available)).ToList() ?? [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to reach InventoryService for product {ProductId}", productId);
            return [];
        }
    }

    public async Task<Guid?> ReserveAsync(Guid productId, Guid locationId, int quantity, string referenceId, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync("/api/inventory/reservations", new
            {
                productId,
                locationId,
                quantity,
                referenceId
            }, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to reserve stock for product {ProductId} at location {LocationId}", productId, locationId);
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var reservation = await response.Content.ReadFromJsonAsync<ReservationResponse>(cancellationToken: cancellationToken);
        return reservation?.Id;
    }

    public async Task<bool> ReleaseAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/inventory/reservations/{reservationId}/release", null, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to release reservation {ReservationId}", reservationId);
            return false;
        }
    }

    public async Task<bool> CommitAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/inventory/reservations/{reservationId}/commit", null, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to commit reservation {ReservationId}", reservationId);
            return false;
        }
    }

    private class InventoryItemResponse
    {
        [JsonPropertyName("locationId")]
        public Guid LocationId { get; set; }

        [JsonPropertyName("available")]
        public int Available { get; set; }
    }

    private class ReservationResponse
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
    }
}
