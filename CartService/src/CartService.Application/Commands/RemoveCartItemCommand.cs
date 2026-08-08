using CartService.Application.Common;
using CartService.Application.Dtos;
using MediatR;

namespace CartService.Application.Commands;

public record RemoveCartItemCommand(Guid CartId, Guid ProductId) : IRequest<ServiceResult<CartDto>>;
