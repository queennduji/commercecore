using CartService.Application.Common;
using CartService.Application.Dtos;
using MediatR;

namespace CartService.Application.Commands;

/// <summary>Creates a new guest cart with a fresh, server-generated Id.</summary>
public record CreateCartCommand : IRequest<ServiceResult<CartDto>>;
