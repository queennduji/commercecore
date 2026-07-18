using AuthenticationService.Application.Commands;
using AuthenticationService.Application.Interfaces;
using AuthenticationService.Domain.Entities;
using AuthenticationService.Infrastructure.Handlers;
using NSubstitute;

namespace AuthenticationService.UnitTests.Handlers;

public class RevokeTokenCommandHandlerTests
{
    [Fact]
    public async Task Handle_ActiveToken_RevokesAndReturnsSuccess()
    {
        var repository = Substitute.For<IRefreshTokenRepository>();
        var activeToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "active-token",
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        repository.GetByTokenAsync("active-token", Arg.Any<CancellationToken>()).Returns(activeToken);

        var handler = new RevokeTokenCommandHandler(repository);
        var result = await handler.Handle(new RevokeTokenCommand("active-token"), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.True(result.Value);
        Assert.NotNull(activeToken.RevokedAt);
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownToken_ReturnsFailure()
    {
        var repository = Substitute.For<IRefreshTokenRepository>();
        repository.GetByTokenAsync("missing-token", Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

        var handler = new RevokeTokenCommandHandler(repository);
        var result = await handler.Handle(new RevokeTokenCommand("missing-token"), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("Invalid or expired refresh token.", result.Errors);
    }

    [Fact]
    public async Task Handle_AlreadyRevokedToken_ReturnsFailure()
    {
        var repository = Substitute.For<IRefreshTokenRepository>();
        var revokedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "already-revoked",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            ExpiresAt = DateTime.UtcNow.AddDays(6),
            RevokedAt = DateTime.UtcNow.AddMinutes(-1)
        };
        repository.GetByTokenAsync("already-revoked", Arg.Any<CancellationToken>()).Returns(revokedToken);

        var handler = new RevokeTokenCommandHandler(repository);
        var result = await handler.Handle(new RevokeTokenCommand("already-revoked"), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("Invalid or expired refresh token.", result.Errors);
    }
}
