using System.Net;
using ApiGateway.IntegrationTests.Fixtures;

namespace ApiGateway.IntegrationTests;

/// <summary>Uses its own isolated fixture (own WebApplicationFactory, own rate-limiter state,
/// own tiny PermitLimit) rather than sharing <see cref="Fixtures.ApiGatewayFixture"/> — sharing
/// state with the routing/auth tests would make this order-dependent on how many requests those
/// tests happened to send first.</summary>
[Collection("ApiGatewayRateLimited")]
public class RateLimitingTests
{
    private readonly RateLimitedApiGatewayFixture _fixture;

    public RateLimitingTests(RateLimitedApiGatewayFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RequestsWithinLimit_Succeed_ThenExceedingRequest_Returns429WithRetryAfter()
    {
        var client = _fixture.Factory.CreateClient();

        for (var i = 0; i < RateLimitedApiGatewayFixture.PermitLimit; i++)
        {
            var response = await client.GetAsync("/api/products");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var rejected = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.True(rejected.Headers.Contains("Retry-After"), "Expected a Retry-After header on the 429 response.");
    }
}
