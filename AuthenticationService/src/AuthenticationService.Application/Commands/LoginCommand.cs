using AuthenticationService.Application.Common;
using AuthenticationService.Application.Dtos;
using MediatR;

namespace AuthenticationService.Application.Commands;

public record LoginCommand(string Email, string Password) : IRequest<ServiceResult<AuthTokens>>;
