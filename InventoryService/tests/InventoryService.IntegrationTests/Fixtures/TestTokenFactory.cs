using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace InventoryService.IntegrationTests.Fixtures;

public static class TestTokenFactory
{
    public static string CreateAccessToken()
    {
        var key = new SymmetricSecurityKey(Convert.FromBase64String(InventoryApiFixture.JwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, "test-admin@example.com")
        };

        var token = new JwtSecurityToken(
            issuer: InventoryApiFixture.JwtIssuer,
            audience: InventoryApiFixture.JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
