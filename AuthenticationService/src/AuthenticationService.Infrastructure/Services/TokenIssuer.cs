using AuthenticationService.Application.Dtos;
using AuthenticationService.Application.Interfaces;
using AuthenticationService.Domain.Entities;
using AuthenticationService.Infrastructure.Identity;
using AuthenticationService.Infrastructure.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AuthenticationService.Infrastructure.Services;

public class TokenIssuer
{
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtOptions _jwtOptions;

    public TokenIssuer(
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository,
        UserManager<ApplicationUser> userManager,
        IOptions<JwtOptions> jwtOptions)
    {
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _userManager = userManager;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthTokens> IssueTokensAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, expiresAt) = _tokenService.GenerateAccessToken(user.Id, user.Email!, roles);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();

        await _refreshTokenRepository.AddAsync(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenValue,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays)
        }, cancellationToken);

        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return new AuthTokens(accessToken, refreshTokenValue, expiresAt);
    }
}
