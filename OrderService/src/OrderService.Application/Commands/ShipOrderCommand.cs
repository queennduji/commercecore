using OrderService.Application.Common;
using OrderService.Application.Dtos;
using MediatR;

namespace OrderService.Application.Commands;

/// <summary>Ops action (any authenticated caller — no ownership check, no role system exists yet
/// to gate this to fulfillment staff specifically). Commits every line's stock reservation, since
/// this is the point the stock actually leaves the building.</summary>
public record ShipOrderCommand(Guid OrderId) : IRequest<ServiceResult<OrderDto>>;
