using AuthenticationService.Application.Common;
using MediatR;

namespace AuthenticationService.Application.Commands;

public record RevokeTokenCommand(string RefreshToken) : IRequest<ServiceResult<bool>>;
