using CartService.Application.Common;
using CartService.Application.Dtos;
using MediatR;

namespace CartService.Application.Commands;

/// <summary>CallerUserId: see GetCartQuery's doc comment.</summary>
public record UpdateCartItemQuantityCommand(Guid CartId, Guid ProductId, int Quantity, Guid? CallerUserId) : IRequest<ServiceResult<CartDto>>;
