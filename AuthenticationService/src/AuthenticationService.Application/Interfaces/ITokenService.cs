namespace AuthenticationService.Application.Interfaces;

public interface ITokenService
{
    (string AccessToken, DateTime ExpiresAt) GenerateAccessToken(Guid userId, string email);

    string GenerateRefreshToken();
}
