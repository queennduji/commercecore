using AuthenticationService.Application.Commands;
using AuthenticationService.Application.Interfaces;
using AuthenticationService.Domain.Entities;
using AuthenticationService.Infrastructure.Handlers;
using AuthenticationService.Infrastructure.Identity;
using AuthenticationService.Infrastructure.Services;
using AuthenticationService.UnitTests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NSubstitute;
using JwtOptions = AuthenticationService.Infrastructure.Options.JwtOptions;

namespace AuthenticationService.UnitTests.Handlers;

public class RefreshTokenCommandHandlerTests
{
    private static (RefreshTokenCommandHandler Handler, IRefreshTokenRepository Repository) CreateHandler(UserManager<ApplicationUser> userManager)
    {
        var tokenService = Substitute.For<ITokenService>();
        tokenService.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<string>())
            .Returns(("new-access-token", DateTime.UtcNow.AddMinutes(15)));
        tokenService.GenerateRefreshToken().Returns("new-refresh-token");

        var refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        var jwtOptions = Options.Create(new JwtOptions { RefreshTokenDays = 7 });
        var tokenIssuer = new TokenIssuer(tokenService, refreshTokenRepository, jwtOptions);

        var handler = new RefreshTokenCommandHandler(userManager, tokenIssuer, refreshTokenRepository);
        return (handler, refreshTokenRepository);
    }

    [Fact]
    public async Task Handle_ActiveToken_RotatesAndReturnsNewTokens()
    {
        var userManager = IdentityTestFactory.CreateUserManager(out _);
        var user = new ApplicationUser { UserName = "refresh@example.com", Email = "refresh@example.com" };
        await userManager.CreateAsync(user, "P@ssw0rd123!");

        var existingToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "old-refresh-token",
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        var (handler, repository) = CreateHandler(userManager);
        repository.GetByTokenAsync("old-refresh-token", Arg.Any<CancellationToken>()).Returns(existingToken);

        var result = await handler.Handle(new RefreshTokenCommand("old-refresh-token"), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("new-access-token", result.Value!.AccessToken);
        Assert.Equal("new-refresh-token", result.Value.RefreshToken);
        Assert.NotNull(existingToken.RevokedAt);
        Assert.Equal("new-refresh-token", existingToken.ReplacedByToken);
    }

    [Fact]
    public async Task Handle_UnknownToken_ReturnsFailure()
    {
        var userManager = IdentityTestFactory.CreateUserManager(out _);
        var (handler, repository) = CreateHandler(userManager);
        repository.GetByTokenAsync("missing-token", Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

        var result = await handler.Handle(new RefreshTokenCommand("missing-token"), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("Invalid or expired refresh token.", result.Errors);
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsFailure()
    {
        var userManager = IdentityTestFactory.CreateUserManager(out _);
        var (handler, repository) = CreateHandler(userManager);

        var expiredToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "expired-token",
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            ExpiresAt = DateTime.UtcNow.AddDays(-3)
        };
        repository.GetByTokenAsync("expired-token", Arg.Any<CancellationToken>()).Returns(expiredToken);

        var result = await handler.Handle(new RefreshTokenCommand("expired-token"), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("Invalid or expired refresh token.", result.Errors);
    }

    [Fact]
    public async Task Handle_RevokedToken_ReturnsFailure()
    {
        var userManager = IdentityTestFactory.CreateUserManager(out _);
        var (handler, repository) = CreateHandler(userManager);

        var revokedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "revoked-token",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            ExpiresAt = DateTime.UtcNow.AddDays(6),
            RevokedAt = DateTime.UtcNow.AddMinutes(-1)
        };
        repository.GetByTokenAsync("revoked-token", Arg.Any<CancellationToken>()).Returns(revokedToken);

        var result = await handler.Handle(new RefreshTokenCommand("revoked-token"), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("Invalid or expired refresh token.", result.Errors);
    }

    [Fact]
    public async Task Handle_TokenForDeletedUser_ReturnsFailure()
    {
        var userManager = IdentityTestFactory.CreateUserManager(out _);
        var (handler, repository) = CreateHandler(userManager);

        var orphanedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "orphaned-token",
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        repository.GetByTokenAsync("orphaned-token", Arg.Any<CancellationToken>()).Returns(orphanedToken);

        var result = await handler.Handle(new RefreshTokenCommand("orphaned-token"), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("Invalid or expired refresh token.", result.Errors);
    }
}
