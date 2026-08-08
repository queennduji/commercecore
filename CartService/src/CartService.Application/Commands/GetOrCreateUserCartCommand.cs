using CartService.Application.Common;
using CartService.Application.Dtos;
using MediatR;

namespace CartService.Application.Commands;

/// <summary>Gets the authenticated caller's own cart (Id == UserId, deterministic), creating an
/// empty one if this is their first visit.</summary>
public record GetOrCreateUserCartCommand(Guid UserId) : IRequest<ServiceResult<CartDto>>;
