using AuthenticationService.Domain.Entities;

namespace AuthenticationService.UnitTests.Domain;

public class RefreshTokenTests
{
    private static RefreshToken CreateToken(DateTime expiresAt, DateTime? revokedAt = null)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "token-value",
            CreatedAt = DateTime.UtcNow.AddMinutes(-1),
            ExpiresAt = expiresAt,
            RevokedAt = revokedAt
        };
    }

    [Fact]
    public void IsActive_WhenNotExpiredAndNotRevoked_ReturnsTrue()
    {
        var token = CreateToken(DateTime.UtcNow.AddDays(1));

        Assert.False(token.IsExpired);
        Assert.False(token.IsRevoked);
        Assert.True(token.IsActive);
    }

    [Fact]
    public void IsActive_WhenExpired_ReturnsFalse()
    {
        var token = CreateToken(DateTime.UtcNow.AddMinutes(-1));

        Assert.True(token.IsExpired);
        Assert.False(token.IsRevoked);
        Assert.False(token.IsActive);
    }

    [Fact]
    public void IsActive_WhenRevoked_ReturnsFalse()
    {
        var token = CreateToken(DateTime.UtcNow.AddDays(1), revokedAt: DateTime.UtcNow.AddMinutes(-1));

        Assert.False(token.IsExpired);
        Assert.True(token.IsRevoked);
        Assert.False(token.IsActive);
    }
}
