using CartService.Application.Common;
using CartService.Application.Dtos;
using MediatR;

namespace CartService.Application.Commands;

/// <summary>CallerUserId: see GetCartQuery's doc comment.</summary>
public record RemoveCartItemCommand(Guid CartId, Guid ProductId, Guid? CallerUserId) : IRequest<ServiceResult<CartDto>>;
