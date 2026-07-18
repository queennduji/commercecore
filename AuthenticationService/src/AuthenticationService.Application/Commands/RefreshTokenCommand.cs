using AuthenticationService.Application.Common;
using AuthenticationService.Application.Dtos;
using MediatR;

namespace AuthenticationService.Application.Commands;

public record RefreshTokenCommand(string RefreshToken) : IRequest<ServiceResult<AuthTokens>>;
