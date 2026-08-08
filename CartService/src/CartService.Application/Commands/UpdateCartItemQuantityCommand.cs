using CartService.Application.Common;
using CartService.Application.Dtos;
using MediatR;

namespace CartService.Application.Commands;

public record UpdateCartItemQuantityCommand(Guid CartId, Guid ProductId, int Quantity) : IRequest<ServiceResult<CartDto>>;
