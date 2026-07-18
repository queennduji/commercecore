using AuthenticationService.Application.Commands;
using AuthenticationService.Application.Dtos;
using AuthenticationService.Application.Interfaces;
using AuthenticationService.Domain.Entities;
using AuthenticationService.Domain.Events;
using AuthenticationService.Infrastructure.Handlers;
using AuthenticationService.Infrastructure.Services;
using AuthenticationService.UnitTests.Support;
using Microsoft.Extensions.Options;
using NSubstitute;
using JwtOptions = AuthenticationService.Infrastructure.Options.JwtOptions;

namespace AuthenticationService.UnitTests.Handlers;

public class RegisterCommandHandlerTests
{
    private static RegisterCommandHandler CreateHandler(
        out IEventPublisher eventPublisher,
        out IRefreshTokenRepository refreshTokenRepository)
    {
        var userManager = IdentityTestFactory.CreateUserManager(out _);

        var tokenService = Substitute.For<ITokenService>();
        tokenService.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<string>())
            .Returns(("access-token", DateTime.UtcNow.AddMinutes(15)));
        tokenService.GenerateRefreshToken().Returns("refresh-token");

        refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        eventPublisher = Substitute.For<IEventPublisher>();

        var jwtOptions = Options.Create(new JwtOptions { RefreshTokenDays = 7 });
        var tokenIssuer = new TokenIssuer(tokenService, refreshTokenRepository, jwtOptions);

        return new RegisterCommandHandler(userManager, tokenIssuer, eventPublisher);
    }

    [Fact]
    public async Task Handle_NewEmail_ReturnsSuccessWithTokensAndPublishesEvent()
    {
        var handler = CreateHandler(out var eventPublisher, out var refreshTokenRepository);
        var command = new RegisterCommand("new-user@example.com", "P@ssw0rd123!");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("access-token", result.Value!.AccessToken);
        Assert.Equal("refresh-token", result.Value.RefreshToken);
        await eventPublisher.Received(1).PublishUserRegisteredAsync(
            Arg.Is<UserRegisteredEvent>(e => e!.Email == "new-user@example.com"),
            Arg.Any<CancellationToken>());
        await refreshTokenRepository.Received(1).AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsFailure()
    {
        var handler = CreateHandler(out var eventPublisher, out _);
        var command = new RegisterCommand("dup-user@example.com", "P@ssw0rd123!");

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        Assert.True(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.Contains("Email is already registered.", second.Errors);
        await eventPublisher.Received(1).PublishUserRegisteredAsync(Arg.Any<UserRegisteredEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WeakPassword_ReturnsFailureWithIdentityErrors()
    {
        var handler = CreateHandler(out _, out _);
        var command = new RegisterCommand("weak-password@example.com", "abc");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);
    }
}
