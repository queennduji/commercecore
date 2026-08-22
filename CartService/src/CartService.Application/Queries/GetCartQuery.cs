using CartService.Application.Common;
using CartService.Application.Dtos;
using MediatR;

namespace CartService.Application.Queries;

/// <summary>CallerUserId is the caller's own JWT-derived user id if authenticated, null if not -
/// never a client-supplied value (see CartsController.GetCallerUserIdOrNull). Required to
/// distinguish "an anonymous guest cart, where possessing the unguessable id IS the
/// authorization" from "an authenticated user's persistent cart, whose id is their own user id
/// and therefore not a secret" - see GetCartQueryHandler.</summary>
public record GetCartQuery(Guid CartId, Guid? CallerUserId) : IRequest<ServiceResult<CartDto>>;
