using CartService.Application.Common;
using MediatR;

namespace CartService.Application.Commands;

/// <summary>CallerUserId: see GetCartQuery's doc comment for why this exists.</summary>
public record ClearCartCommand(Guid CartId, Guid? CallerUserId) : IRequest<ServiceResult<bool>>;
