using OrderService.Application.Common;
using OrderService.Application.Dtos;
using MediatR;

namespace OrderService.Application.Commands;

/// <summary>Checks out the caller's own cart (fetched from CartService by UserId, per its
/// deterministic-authenticated-cart convention) – reserves stock for every line item across
/// InventoryService's locations, creates the order, and clears the cart.</summary>
public record CheckoutCommand(Guid UserId, string ShippingAddress) : IRequest<ServiceResult<OrderDto>>;
