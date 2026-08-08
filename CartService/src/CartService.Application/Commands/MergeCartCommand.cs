using CartService.Application.Common;
using CartService.Application.Dtos;
using MediatR;

namespace CartService.Application.Commands;

/// <summary>Merges a guest cart's items into the authenticated caller's own cart (summing
/// quantities for duplicate products), then deletes the guest cart. Typically called right after
/// login with the guest cart the shopper was using pre-auth.</summary>
public record MergeCartCommand(Guid UserId, Guid SourceCartId) : IRequest<ServiceResult<CartDto>>;
