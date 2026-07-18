using System.Net;
using System.Net.Http.Json;
using AuthenticationService.Application.Commands;
using AuthenticationService.Application.Dtos;
using AuthenticationService.IntegrationTests.Fixtures;

namespace AuthenticationService.IntegrationTests;

[Collection("AuthApi")]
public class AuthEndpointsTests
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(AuthApiFixture fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    private static string UniqueEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@example.com";

    [Fact]
    public async Task Register_NewEmail_ReturnsOkWithTokens()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterCommand(UniqueEmail("register"), "P@ssw0rd123!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokens = await response.Content.ReadFromJsonAsync<AuthTokens>();
        Assert.NotNull(tokens);
        Assert.False(string.IsNullOrWhiteSpace(tokens!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        var email = UniqueEmail("duplicate");
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterCommand(email, "P@ssw0rd123!"));

        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterCommand(email, "P@ssw0rd123!"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithTokens()
    {
        var email = UniqueEmail("login");
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterCommand(email, "P@ssw0rd123!"));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginCommand(email, "P@ssw0rd123!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokens = await response.Content.ReadFromJsonAsync<AuthTokens>();
        Assert.NotNull(tokens);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        var email = UniqueEmail("badlogin");
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterCommand(email, "P@ssw0rd123!"));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginCommand(email, "WrongPassword!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_ValidToken_ReturnsOkWithRotatedTokens()
    {
        var email = UniqueEmail("refresh");
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterCommand(email, "P@ssw0rd123!"));
        var originalTokens = await registerResponse.Content.ReadFromJsonAsync<AuthTokens>();

        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenCommand(originalTokens!.RefreshToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rotatedTokens = await response.Content.ReadFromJsonAsync<AuthTokens>();
        Assert.NotNull(rotatedTokens);
        Assert.NotEqual(originalTokens.RefreshToken, rotatedTokens!.RefreshToken);
    }

    [Fact]
    public async Task Refresh_InvalidToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenCommand("not-a-real-token"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Revoke_ValidToken_ReturnsNoContent()
    {
        var email = UniqueEmail("revoke");
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterCommand(email, "P@ssw0rd123!"));
        var tokens = await registerResponse.Content.ReadFromJsonAsync<AuthTokens>();

        var response = await _client.PostAsJsonAsync("/api/auth/revoke", new RevokeTokenCommand(tokens!.RefreshToken));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Revoke_InvalidToken_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/revoke", new RevokeTokenCommand("not-a-real-token"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
