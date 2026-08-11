using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PaymentService.Application.Dtos;
using PaymentService.IntegrationTests.Fixtures;

namespace PaymentService.IntegrationTests;

[Collection("PaymentApi")]
public class PaymentEndpointsTests
{
    private readonly PaymentApiFixture _fixture;

    public PaymentEndpointsTests(PaymentApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Charge_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _fixture.Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/payments/charge", new { orderId = Guid.NewGuid(), amount = 10m, currency = "usd", paymentMethodId = "pm_card_visa" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Charge_ValidCard_RecordsSucceededPaymentOwnedByCaller()
    {
        var userId = Guid.NewGuid();
        var client = AuthedClient(userId);
        var orderId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync("/api/payments/charge", new { orderId, amount = 42.50m, currency = "usd", paymentMethodId = "pm_card_visa" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payment = await response.Content.ReadFromJsonAsync<PaymentDto>();
        Assert.Equal(userId, payment!.UserId);
        Assert.Equal(orderId, payment.OrderId);
        Assert.Equal("Succeeded", payment.Status);
        Assert.NotNull(payment.ProviderReference);
        Assert.Contains(_fixture.PaymentGateway.Charges, c => c.Amount == 42.50m && c.PaymentMethodId == "pm_card_visa");
    }

    [Fact]
    public async Task Charge_DeclinedCard_ReturnsBadRequestButStillRecordsFailedPayment()
    {
        var userId = Guid.NewGuid();
        var client = AuthedClient(userId);
        var orderId = Guid.NewGuid();
        _fixture.PaymentGateway.DeclinedPaymentMethodIds.Add("pm_card_visa_chargeDeclined");

        var response = await client.PostAsJsonAsync("/api/payments/charge", new { orderId, amount = 15m, currency = "usd", paymentMethodId = "pm_card_visa_chargeDeclined" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var listResponse = await client.GetAsync($"/api/payments/order/{orderId}");
        var payments = await listResponse.Content.ReadFromJsonAsync<List<PaymentDto>>();
        var recorded = Assert.Single(payments!);
        Assert.Equal("Failed", recorded.Status);
        Assert.Equal("Your card was declined.", recorded.FailureReason);
    }

    [Fact]
    public async Task Refund_AfterSuccessfulCharge_TransitionsToRefunded()
    {
        var userId = Guid.NewGuid();
        var client = AuthedClient(userId);
        var orderId = Guid.NewGuid();

        var chargeResponse = await client.PostAsJsonAsync("/api/payments/charge", new { orderId, amount = 30m, currency = "usd", paymentMethodId = "pm_card_visa" });
        var charged = await chargeResponse.Content.ReadFromJsonAsync<PaymentDto>();

        var refundResponse = await client.PostAsJsonAsync("/api/payments/refund", new { orderId });

        Assert.Equal(HttpStatusCode.OK, refundResponse.StatusCode);
        var refunded = await refundResponse.Content.ReadFromJsonAsync<PaymentDto>();
        Assert.Equal(charged!.Id, refunded!.Id);
        Assert.Equal("Refunded", refunded.Status);
        Assert.Contains(charged.ProviderReference, _fixture.PaymentGateway.Refunds);
    }

    [Fact]
    public async Task Refund_NoSuccessfulPaymentForOrder_ReturnsBadRequest()
    {
        var client = AuthedClient(Guid.NewGuid());

        var response = await client.PostAsJsonAsync("/api/payments/refund", new { orderId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_PaymentOwnedByDifferentUser_ReturnsNotFound()
    {
        var ownerId = Guid.NewGuid();
        var ownerClient = AuthedClient(ownerId);
        var chargeResponse = await ownerClient.PostAsJsonAsync("/api/payments/charge", new { orderId = Guid.NewGuid(), amount = 5m, currency = "usd", paymentMethodId = "pm_card_visa" });
        var payment = await chargeResponse.Content.ReadFromJsonAsync<PaymentDto>();

        var strangerClient = AuthedClient(Guid.NewGuid());
        var response = await strangerClient.GetAsync($"/api/payments/{payment!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListByOrder_OnlyReturnsPaymentsOwnedByCaller()
    {
        var orderId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var ownerClient = AuthedClient(ownerId);
        await ownerClient.PostAsJsonAsync("/api/payments/charge", new { orderId, amount = 8m, currency = "usd", paymentMethodId = "pm_card_visa" });

        var strangerClient = AuthedClient(Guid.NewGuid());
        var response = await strangerClient.GetAsync($"/api/payments/order/{orderId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payments = await response.Content.ReadFromJsonAsync<List<PaymentDto>>();
        Assert.Empty(payments!);
    }

    private HttpClient AuthedClient(Guid userId)
    {
        var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokenFactory.CreateAccessToken(userId));
        return client;
    }
}
