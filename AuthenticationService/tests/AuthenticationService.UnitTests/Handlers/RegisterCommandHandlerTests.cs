using AuthenticationService.Application.Commands;
using AuthenticationService.Application.Dtos;
using AuthenticationService.Application.Interfaces;
using AuthenticationService.Domain.Entities;
using AuthenticationService.Domain.Events;
using AuthenticationService.Infrastructure.Handlers;
using AuthenticationService.Infrastructure.Identity;
using AuthenticationService.Infrastructure.Services;
using AuthenticationService.UnitTests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NSubstitute;
using AdminOptions = AuthenticationService.Infrastructure.Options.AdminOptions;
using JwtOptions = AuthenticationService.Infrastructure.Options.JwtOptions;

namespace AuthenticationService.UnitTests.Handlers;

public class RegisterCommandHandlerTests
{
    private static RegisterCommandHandler CreateHandler(
        out IEventPublisher eventPublisher,
        out IRefreshTokenRepository refreshTokenRepository,
        out UserManager<ApplicationUser> userManager,
        string[]? adminEmails = null)
    {
        userManager = IdentityTestFactory.CreateUserManager(out _);

        var tokenService = Substitute.For<ITokenService>();
        tokenService.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>())
            .Returns(("access-token", DateTime.UtcNow.AddMinutes(15)));
        tokenService.GenerateRefreshToken().Returns("refresh-token");

        refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        eventPublisher = Substitute.For<IEventPublisher>();

        var jwtOptions = Options.Create(new JwtOptions { RefreshTokenDays = 7 });
        var tokenIssuer = new TokenIssuer(tokenService, refreshTokenRepository, userManager, jwtOptions);
        var adminOptions = Options.Create(new AdminOptions { Emails = adminEmails ?? [] });

        return new RegisterCommandHandler(userManager, tokenIssuer, eventPublisher, adminOptions);
    }

    [Fact]
    public async Task Handle_NewEmail_ReturnsSuccessWithTokensAndPublishesEvent()
    {
        var handler = CreateHandler(out var eventPublisher, out var refreshTokenRepository, out _);
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
        var handler = CreateHandler(out var eventPublisher, out _, out _);
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
        var handler = CreateHandler(out _, out _, out _);
        var command = new RegisterCommand("weak-password@example.com", "abc");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Handle_WithPhoneNumber_PublishesEventWithPhoneNumber()
    {
        var handler = CreateHandler(out var eventPublisher, out _, out _);
        var command = new RegisterCommand("with-phone@example.com", "P@ssw0rd123!", "+15551234567");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        await eventPublisher.Received(1).PublishUserRegisteredAsync(
            Arg.Is<UserRegisteredEvent>(e => e != null && e.PhoneNumber == "+15551234567"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithoutPhoneNumber_PublishesEventWithNullPhoneNumber()
    {
        var handler = CreateHandler(out var eventPublisher, out _, out _);
        var command = new RegisterCommand("no-phone@example.com", "P@ssw0rd123!");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        await eventPublisher.Received(1).PublishUserRegisteredAsync(
            Arg.Is<UserRegisteredEvent>(e => e != null && e.PhoneNumber == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmailMatchesConfiguredAdminEmail_AssignsAdminRoleImmediately()
    {
        // Covers the case AdminRoleSeeder (startup-only) can't: registering for the first time
        // with a configured admin email should not have to wait for the next restart to get the
        // role. Pre-creates the "Admin" role directly here since this test doesn't run
        // AdminRoleSeeder - mirrors what it does at real startup.
        var userManager = IdentityTestFactory.CreateUserManagerWithRoles(out var roleManager);
        await roleManager.CreateAsync(new IdentityRole<Guid>(AdminOptions.RoleName));

        var tokenService = Substitute.For<ITokenService>();
        tokenService.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>())
            .Returns(("access-token", DateTime.UtcNow.AddMinutes(15)));
        tokenService.GenerateRefreshToken().Returns("refresh-token");
        var jwtOptions = Options.Create(new JwtOptions { RefreshTokenDays = 7 });
        var tokenIssuer = new TokenIssuer(tokenService, Substitute.For<IRefreshTokenRepository>(), userManager, jwtOptions);
        var adminOptions = Options.Create(new AdminOptions { Emails = ["admin@example.com"] });

        var handler = new RegisterCommandHandler(userManager, tokenIssuer, Substitute.For<IEventPublisher>(), adminOptions);
        var result = await handler.Handle(new RegisterCommand("admin@example.com", "P@ssw0rd123!"), CancellationToken.None);

        Assert.True(result.Succeeded);
        var user = await userManager.FindByEmailAsync("admin@example.com");
        Assert.True(await userManager.IsInRoleAsync(user!, AdminOptions.RoleName));
    }

    [Fact]
    public async Task Handle_EmailNotInAdminList_DoesNotAssignAdminRole()
    {
        var userManager = IdentityTestFactory.CreateUserManagerWithRoles(out var roleManager);
        await roleManager.CreateAsync(new IdentityRole<Guid>(AdminOptions.RoleName));

        var tokenService = Substitute.For<ITokenService>();
        tokenService.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>())
            .Returns(("access-token", DateTime.UtcNow.AddMinutes(15)));
        tokenService.GenerateRefreshToken().Returns("refresh-token");
        var jwtOptions = Options.Create(new JwtOptions { RefreshTokenDays = 7 });
        var tokenIssuer = new TokenIssuer(tokenService, Substitute.For<IRefreshTokenRepository>(), userManager, jwtOptions);
        var adminOptions = Options.Create(new AdminOptions { Emails = ["admin@example.com"] });

        var handler = new RegisterCommandHandler(userManager, tokenIssuer, Substitute.For<IEventPublisher>(), adminOptions);
        var result = await handler.Handle(new RegisterCommand("regular-user@example.com", "P@ssw0rd123!"), CancellationToken.None);

        Assert.True(result.Succeeded);
        var user = await userManager.FindByEmailAsync("regular-user@example.com");
        Assert.False(await userManager.IsInRoleAsync(user!, AdminOptions.RoleName));
    }
}
