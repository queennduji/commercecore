using System.Net.Http.Json;
using System.Text.Json.Serialization;
using OrderService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace OrderService.Infrastructure.Clients;

/// <summary>Synchronous HTTP call to PaymentService. Uses ForwardAuthorizationHandler (registered
/// in DependencyInjection.cs) to carry the caller's own JWT, since PaymentService's charge/refund
/// endpoints require [Authorize].</summary>
public class PaymentServiceClient : IPaymentServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentServiceClient> _logger;

    public PaymentServiceClient(HttpClient httpClient, ILogger<PaymentServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PaymentResult> ChargeAsync(Guid orderId, decimal amount, string currency, string paymentMethodId, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync("/api/payments/charge", new { orderId, amount, currency, paymentMethodId }, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to reach PaymentService to charge order {OrderId}", orderId);
            return new PaymentResult(false, "Failed to reach PaymentService.");
        }

        if (response.IsSuccessStatusCode)
        {
            return new PaymentResult(true, null);
        }

        var errorBody = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: cancellationToken);
        return new PaymentResult(false, errorBody?.Errors?.FirstOrDefault() ?? "Payment failed.");
    }

    public async Task<PaymentResult> RefundAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync("/api/payments/refund", new { orderId }, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to reach PaymentService to refund order {OrderId}", orderId);
            return new PaymentResult(false, "Failed to reach PaymentService.");
        }

        if (response.IsSuccessStatusCode)
        {
            return new PaymentResult(true, null);
        }

        var errorBody = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: cancellationToken);
        return new PaymentResult(false, errorBody?.Errors?.FirstOrDefault() ?? "Refund failed.");
    }

    private class ErrorResponse
    {
        [JsonPropertyName("errors")]
        public List<string>? Errors { get; set; }
    }
}
