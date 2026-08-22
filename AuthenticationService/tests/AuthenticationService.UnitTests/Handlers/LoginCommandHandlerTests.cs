using AuthenticationService.Application.Commands;
using AuthenticationService.Application.Interfaces;
using AuthenticationService.Domain.Events;
using AuthenticationService.Infrastructure.Handlers;
using AuthenticationService.Infrastructure.Identity;
using AuthenticationService.Infrastructure.Services;
using AuthenticationService.UnitTests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NSubstitute;
using JwtOptions = AuthenticationService.Infrastructure.Options.JwtOptions;

namespace AuthenticationService.UnitTests.Handlers;

public class LoginCommandHandlerTests
{
    private static LoginCommandHandler CreateHandler(
        UserManager<ApplicationUser> userManager,
        out IEventPublisher eventPublisher)
    {
        var tokenService = Substitute.For<ITokenService>();
        tokenService.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>())
            .Returns(("access-token", DateTime.UtcNow.AddMinutes(15)));
        tokenService.GenerateRefreshToken().Returns("refresh-token");

        var refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        eventPublisher = Substitute.For<IEventPublisher>();

        var jwtOptions = Options.Create(new JwtOptions { RefreshTokenDays = 7 });
        var tokenIssuer = new TokenIssuer(tokenService, refreshTokenRepository, userManager, jwtOptions);

        return new LoginCommandHandler(userManager, tokenIssuer, eventPublisher);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsSuccessAndPublishesEvent()
    {
        var userManager = IdentityTestFactory.CreateUserManager(out _);
        await userManager.CreateAsync(new ApplicationUser { UserName = "login@example.com", Email = "login@example.com" }, "P@ssw0rd123!");

        var handler = CreateHandler(userManager, out var eventPublisher);
        var result = await handler.Handle(new LoginCommand("login@example.com", "P@ssw0rd123!"), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("access-token", result.Value!.AccessToken);
        await eventPublisher.Received(1).PublishUserLoggedInAsync(Arg.Any<UserLoggedInEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownEmail_ReturnsFailure()
    {
        var userManager = IdentityTestFactory.CreateUserManager(out _);
        var handler = CreateHandler(userManager, out var eventPublisher);

        var result = await handler.Handle(new LoginCommand("nobody@example.com", "P@ssw0rd123!"), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("Invalid email or password.", result.Errors);
        await eventPublisher.DidNotReceive().PublishUserLoggedInAsync(Arg.Any<UserLoggedInEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsFailure()
    {
        var userManager = IdentityTestFactory.CreateUserManager(out _);
        await userManager.CreateAsync(new ApplicationUser { UserName = "wrongpw@example.com", Email = "wrongpw@example.com" }, "P@ssw0rd123!");

        var handler = CreateHandler(userManager, out _);
        var result = await handler.Handle(new LoginCommand("wrongpw@example.com", "WrongPassword!"), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("Invalid email or password.", result.Errors);
    }
}
