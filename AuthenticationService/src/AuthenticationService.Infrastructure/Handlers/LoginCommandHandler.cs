using AuthenticationService.Application.Commands;
using AuthenticationService.Application.Common;
using AuthenticationService.Application.Dtos;
using AuthenticationService.Application.Interfaces;
using AuthenticationService.Domain.Events;
using AuthenticationService.Infrastructure.Identity;
using AuthenticationService.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace AuthenticationService.Infrastructure.Handlers;

public class LoginCommandHandler : IRequestHandler<LoginCommand, ServiceResult<AuthTokens>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TokenIssuer _tokenIssuer;
    private readonly IEventPublisher _eventPublisher;

    public LoginCommandHandler(UserManager<ApplicationUser> userManager, TokenIssuer tokenIssuer, IEventPublisher eventPublisher)
    {
        _userManager = userManager;
        _tokenIssuer = tokenIssuer;
        _eventPublisher = eventPublisher;
    }

    public async Task<ServiceResult<AuthTokens>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return ServiceResult<AuthTokens>.Failure("Invalid email or password.");
        }

        var tokens = await _tokenIssuer.IssueTokensAsync(user, cancellationToken);

        await _eventPublisher.PublishUserLoggedInAsync(new UserLoggedInEvent
        {
            UserId = user.Id,
            LoggedInAt = DateTime.UtcNow
        }, cancellationToken);

        return ServiceResult<AuthTokens>.Success(tokens);
    }
}
