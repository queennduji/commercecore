using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace ApiGateway.IntegrationTests.Fixtures;

public static class TestTokenFactory
{
    public static string CreateAccessToken(Guid? userId = null)
    {
        var key = new SymmetricSecurityKey(Convert.FromBase64String(ApiGatewayFixture.JwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, (userId ?? Guid.NewGuid()).ToString()),
            new Claim(JwtRegisteredClaimNames.Email, "test-shopper@example.com")
        };

        var token = new JwtSecurityToken(
            issuer: ApiGatewayFixture.JwtIssuer,
            audience: ApiGatewayFixture.JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
