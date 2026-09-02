using System.Net;
using System.Net.Http.Headers;
using ApiGateway.IntegrationTests.Fixtures;

namespace ApiGateway.IntegrationTests;

/// <summary>Proves the gateway's own logic – per-route AuthorizationPolicy enforcement and
/// request forwarding – against a real YARP pipeline. Which physical backend service each path
/// prefix reaches is proven separately by the live Docker smoke test against real, distinct
/// services; every test here proxies to the same <see cref="FakeBackendServer"/>.</summary>
[Collection("ApiGateway")]
public class GatewayRoutingTests
{
    private readonly ApiGatewayFixture _fixture;

    public GatewayRoutingTests(ApiGatewayFixture fixture)
    {
        _fixture = fixture;
        _fixture.Backend.ReceivedRequests.Clear();
    }

    [Theory]
    [InlineData("/api/orders/00000000-0000-0000-0000-000000000001")]
    [InlineData("/api/payments/00000000-0000-0000-0000-000000000001")]
    [InlineData("/api/shipments/00000000-0000-0000-0000-000000000001")]
    [InlineData("/api/notifications/00000000-0000-0000-0000-000000000001")]
    public async Task ProtectedRoute_NoToken_ReturnsUnauthorizedWithoutReachingBackend(string path)
    {
        var client = _fixture.Factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(_fixture.Backend.ReceivedRequests);
    }

    [Theory]
    [InlineData("/api/orders/00000000-0000-0000-0000-000000000001")]
    [InlineData("/api/payments/00000000-0000-0000-0000-000000000001")]
    [InlineData("/api/shipments/00000000-0000-0000-0000-000000000001")]
    [InlineData("/api/notifications/00000000-0000-0000-0000-000000000001")]
    public async Task ProtectedRoute_ValidToken_ProxiesThroughWithAuthorizationHeaderForwarded(string path)
    {
        var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokenFactory.CreateAccessToken());

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var received = Assert.Single(_fixture.Backend.ReceivedRequests);
        Assert.Equal(path, received.Path);
        Assert.StartsWith("Bearer ", received.AuthorizationHeader);
    }

    [Theory]
    [InlineData("/api/auth/login")]
    [InlineData("/api/products")]
    [InlineData("/api/categories")]
    [InlineData("/api/inventory/00000000-0000-0000-0000-000000000001")]
    [InlineData("/api/locations")]
    [InlineData("/api/carts/00000000-0000-0000-0000-000000000001")]
    public async Task PassThroughRoute_NoToken_StillProxiesThrough(string path)
    {
        var client = _fixture.Factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var received = Assert.Single(_fixture.Backend.ReceivedRequests);
        Assert.Equal(path, received.Path);
    }

    [Fact]
    public async Task PassThroughRoute_WithToken_StillProxiesThroughAndForwardsHeader()
    {
        var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokenFactory.CreateAccessToken());

        var response = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var received = Assert.Single(_fixture.Backend.ReceivedRequests);
        Assert.StartsWith("Bearer ", received.AuthorizationHeader);
    }

    [Fact]
    public async Task ProtectedRoute_InvalidToken_ReturnsUnauthorizedWithoutReachingBackend()
    {
        var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");

        var response = await client.GetAsync("/api/orders/00000000-0000-0000-0000-000000000001");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(_fixture.Backend.ReceivedRequests);
    }

    [Fact]
    public async Task Health_ReturnsOkWithoutAuth()
    {
        var client = _fixture.Factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
