using AuthenticationService.Application.Commands;
using AuthenticationService.Application.Common;
using AuthenticationService.Application.Dtos;
using AuthenticationService.Application.Interfaces;
using AuthenticationService.Domain.Events;
using AuthenticationService.Infrastructure.Identity;
using AuthenticationService.Infrastructure.Options;
using AuthenticationService.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AuthenticationService.Infrastructure.Handlers;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, ServiceResult<AuthTokens>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TokenIssuer _tokenIssuer;
    private readonly IEventPublisher _eventPublisher;
    private readonly AdminOptions _adminOptions;

    public RegisterCommandHandler(
        UserManager<ApplicationUser> userManager,
        TokenIssuer tokenIssuer,
        IEventPublisher eventPublisher,
        IOptions<AdminOptions> adminOptions)
    {
        _userManager = userManager;
        _tokenIssuer = tokenIssuer;
        _eventPublisher = eventPublisher;
        _adminOptions = adminOptions.Value;
    }

    public async Task<ServiceResult<AuthTokens>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            return ServiceResult<AuthTokens>.Failure("Email is already registered.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return ServiceResult<AuthTokens>.Failure(createResult.Errors.Select(e => e.Description).ToArray());
        }

        // Covers registering for the first time with a configured admin email - AdminRoleSeeder
        // only catches already-registered users at startup, so without this a fresh admin
        // registration wouldn't get the role until the next restart.
        if (_adminOptions.Emails.Any(adminEmail => string.Equals(adminEmail, user.Email, StringComparison.OrdinalIgnoreCase)))
        {
            await _userManager.AddToRoleAsync(user, AdminOptions.RoleName);
        }

        var tokens = await _tokenIssuer.IssueTokensAsync(user, cancellationToken);

        await _eventPublisher.PublishUserRegisteredAsync(new UserRegisteredEvent
        {
            UserId = user.Id,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber,
            RegisteredAt = DateTime.UtcNow
        }, cancellationToken);

        return ServiceResult<AuthTokens>.Success(tokens);
    }
}
