using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace CatalogService.IntegrationTests.Fixtures;

public static class TestTokenFactory
{
    public static string CreateAccessToken()
    {
        var key = new SymmetricSecurityKey(Convert.FromBase64String(CatalogApiFixture.JwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, "test-admin@example.com")
        };

        var token = new JwtSecurityToken(
            issuer: CatalogApiFixture.JwtIssuer,
            audience: CatalogApiFixture.JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
