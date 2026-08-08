using CartService.Application.Common;
using CartService.Application.Dtos;
using MediatR;

namespace CartService.Application.Commands;

/// <summary>Adds a product to the cart, or increments its quantity if already present. Snapshots
/// name/sku/price from CatalogService at add-time.</summary>
public record AddCartItemCommand(Guid CartId, Guid ProductId, int Quantity) : IRequest<ServiceResult<CartDto>>;
