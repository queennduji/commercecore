using CartService.Application.Common;
using CartService.Application.Dtos;
using MediatR;

namespace CartService.Application.Queries;

public record GetCartQuery(Guid CartId) : IRequest<ServiceResult<CartDto>>;
