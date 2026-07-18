using System.Net;
using System.Net.Http.Json;
using AuthenticationService.Application.Commands;
using AuthenticationService.Application.Dtos;
using AuthenticationService.IntegrationTests.Fixtures;

namespace AuthenticationService.IntegrationTests;

[Collection("AuthApi")]
public class AuthLifecycleTests
{
    private readonly HttpClient _client;

    public AuthLifecycleTests(AuthApiFixture fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task FullLifecycle_RegisterLoginRefreshRevoke_RevokedTokenIsRejectedOnReuse()
    {
        var email = $"lifecycle-{Guid.NewGuid():N}@example.com";
        const string password = "P@ssw0rd123!";

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterCommand(email, password));
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginCommand(email, password));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginTokens = await loginResponse.Content.ReadFromJsonAsync<AuthTokens>();
        Assert.NotNull(loginTokens);

        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenCommand(loginTokens!.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var refreshedTokens = await refreshResponse.Content.ReadFromJsonAsync<AuthTokens>();
        Assert.NotNull(refreshedTokens);
        Assert.NotEqual(loginTokens.RefreshToken, refreshedTokens!.RefreshToken);

        var revokeResponse = await _client.PostAsJsonAsync("/api/auth/revoke", new RevokeTokenCommand(refreshedTokens.RefreshToken));
        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);

        var reuseResponse = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenCommand(refreshedTokens.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
    }
}
