using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthenticationService.Infrastructure.Options;
using AuthenticationService.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace AuthenticationService.UnitTests.Services;

public class TokenServiceTests
{
    private static TokenService CreateTokenService(int accessTokenMinutes = 15)
    {
        var options = Options.Create(new JwtOptions
        {
            Key = Convert.ToBase64String(new byte[32]),
            Issuer = "CommerceCore.AuthenticationService.Tests",
            Audience = "CommerceCore.Tests",
            AccessTokenMinutes = accessTokenMinutes,
            RefreshTokenDays = 7
        });

        return new TokenService(options);
    }

    [Fact]
    public void GenerateAccessToken_ReturnsJwtWithExpectedClaimsIssuerAndAudience()
    {
        var tokenService = CreateTokenService(accessTokenMinutes: 15);
        var userId = Guid.NewGuid();

        var (accessToken, expiresAt) = tokenService.GenerateAccessToken(userId, "claims@example.com", []);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        Assert.Equal(userId.ToString(), jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("claims@example.com", jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("CommerceCore.AuthenticationService.Tests", jwt.Issuer);
        Assert.Equal("CommerceCore.Tests", jwt.Audiences.Single());
        Assert.True(Math.Abs((jwt.ValidTo - expiresAt).TotalSeconds) < 2);
        Assert.True(Math.Abs((expiresAt - DateTime.UtcNow.AddMinutes(15)).TotalSeconds) < 2);
        Assert.DoesNotContain(jwt.Claims, c => c.Type == ClaimTypes.Role);
    }

    [Fact]
    public void GenerateAccessToken_WithRoles_EmitsOneClaimTypesRoleClaimPerRole()
    {
        // ClaimTypes.Role specifically (not a short "role" string) matters: it's what
        // TokenValidationParameters.RoleClaimType defaults to across every service, which is what
        // makes [Authorize(Roles = "...")] recognize these claims with zero extra per-service
        // config. This test is the thing that would catch a regression to a short claim name.
        var tokenService = CreateTokenService();
        var userId = Guid.NewGuid();

        var (accessToken, _) = tokenService.GenerateAccessToken(userId, "admin@example.com", ["Admin", "Support"]);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        var roleClaims = jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        Assert.Equal(["Admin", "Support"], roleClaims);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsUniqueBase64ValuesAcrossCalls()
    {
        var tokenService = CreateTokenService();

        var first = tokenService.GenerateRefreshToken();
        var second = tokenService.GenerateRefreshToken();

        Assert.NotEqual(first, second);
        Assert.True(Convert.TryFromBase64String(first, new byte[64], out _));
        Assert.True(Convert.TryFromBase64String(second, new byte[64], out _));
    }
}
