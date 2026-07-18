namespace AuthenticationService.Application.Dtos;

public record AuthTokens(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt);
