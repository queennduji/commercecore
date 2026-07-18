using AuthenticationService.Application.Commands;
using AuthenticationService.Application.Common;
using AuthenticationService.Application.Interfaces;
using MediatR;

namespace AuthenticationService.Infrastructure.Handlers;

public class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, ServiceResult<bool>>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public RevokeTokenCommandHandler(IRefreshTokenRepository refreshTokenRepository)
    {
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<ServiceResult<bool>> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var existingToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);
        if (existingToken is null || !existingToken.IsActive)
        {
            return ServiceResult<bool>.Failure("Invalid or expired refresh token.");
        }

        existingToken.RevokedAt = DateTime.UtcNow;
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }
}
