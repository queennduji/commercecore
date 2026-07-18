using AuthenticationService.Application.Commands;
using AuthenticationService.Application.Common;
using AuthenticationService.Application.Dtos;
using AuthenticationService.Application.Interfaces;
using AuthenticationService.Infrastructure.Identity;
using AuthenticationService.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace AuthenticationService.Infrastructure.Handlers;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, ServiceResult<AuthTokens>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TokenIssuer _tokenIssuer;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public RefreshTokenCommandHandler(UserManager<ApplicationUser> userManager, TokenIssuer tokenIssuer, IRefreshTokenRepository refreshTokenRepository)
    {
        _userManager = userManager;
        _tokenIssuer = tokenIssuer;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<ServiceResult<AuthTokens>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var existingToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);
        if (existingToken is null || !existingToken.IsActive)
        {
            return ServiceResult<AuthTokens>.Failure("Invalid or expired refresh token.");
        }

        var user = await _userManager.FindByIdAsync(existingToken.UserId.ToString());
        if (user is null)
        {
            return ServiceResult<AuthTokens>.Failure("Invalid or expired refresh token.");
        }

        var tokens = await _tokenIssuer.IssueTokensAsync(user, cancellationToken);

        existingToken.RevokedAt = DateTime.UtcNow;
        existingToken.ReplacedByToken = tokens.RefreshToken;
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return ServiceResult<AuthTokens>.Success(tokens);
    }
}
