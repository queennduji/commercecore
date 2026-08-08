using CartService.Application.Common;
using MediatR;

namespace CartService.Application.Commands;

public record ClearCartCommand(Guid CartId) : IRequest<ServiceResult<bool>>;
