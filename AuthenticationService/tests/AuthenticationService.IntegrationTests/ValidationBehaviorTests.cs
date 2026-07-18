using System.Net;
using System.Net.Http.Json;
using AuthenticationService.Application.Commands;
using AuthenticationService.IntegrationTests.Fixtures;

namespace AuthenticationService.IntegrationTests;

[Collection("AuthApi")]
public class ValidationBehaviorTests
{
    private readonly HttpClient _client;

    public ValidationBehaviorTests(AuthApiFixture fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task Register_InvalidEmailFormat_ReturnsBadRequestWithValidationError()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterCommand("not-an-email", "P@ssw0rd123!"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("email", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_EmptyPassword_ReturnsBadRequestWithValidationError()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterCommand($"valid-{Guid.NewGuid():N}@example.com", ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_EmptyEmail_ReturnsBadRequestWithValidationError()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginCommand("", "P@ssw0rd123!"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_EmptyToken_ReturnsBadRequestWithValidationError()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenCommand(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Revoke_EmptyToken_ReturnsBadRequestWithValidationError()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/revoke", new RevokeTokenCommand(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
